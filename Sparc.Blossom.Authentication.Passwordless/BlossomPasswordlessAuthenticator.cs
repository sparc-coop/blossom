using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Passwordless;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Net.Http.Json;
using Sparc.Blossom.Authentication;

namespace Sparc.Blossom.Authentication.Passwordless;

public class BlossomPasswordlessAuthenticator<T> : BlossomDefaultAuthenticator<T>
    where T : BlossomUser, new()
{
    readonly Lazy<Task<IJSObjectReference>> Js;
    public NavigationManager Nav { get; }
    IPasswordlessClient PasswordlessClient { get; }
    public HttpClient Client { get; }
    readonly string publicKey;
    public BlossomPasswordlessAuthenticator(
        IPasswordlessClient _passwordlessClient,
        IOptions<PasswordlessOptions> options,
        IRepository<T> users,
        NavigationManager nav,
        IJSRuntime js)
        : base(users)
    {
        PasswordlessClient = _passwordlessClient;
        Js = new(() => js.InvokeAsync<IJSObjectReference>("import", "./_content/Sparc.Blossom.Authentication.Passwordless/BlossomPasswordlessAuthenticator.js").AsTask());
        Client = new HttpClient
        {
            BaseAddress = new Uri("https://v4.passwordless.dev/")
        };
        Client.DefaultRequestHeaders.Add("ApiSecret", options.Value.ApiSecret);
        Nav = nav;

        publicKey = options.Value.ApiKey!;
    }

    public override async IAsyncEnumerable<LoginStates> Login(ClaimsPrincipal principal, string? emailOrToken = null)
    {
        Message = null;
        
        // 1. Convert the ClaimsPrincipal from the cookie into a BlossomUser
        // If the BlossomUser is already attached to Passwordless, they're logged in because their cookie is valid
        var user = await GetAsync(principal);

        if (user.ExternalId != null)
        {
            LoginState = LoginStates.LoggedIn;
            yield return LoginState;
            yield break;
        }
        else
        {
            if (LoginState == LoginStates.NotInitialized && string.IsNullOrEmpty(emailOrToken))
            {
                LoginState = LoginStates.LoggedOut;
                yield return LoginState;
                yield break;
            }
        }

        await InitPasswordlessAsync();

        // 2. BlossomUser is not yet attached to Passwordless. Look for discoverable passkeys on their device.
        emailOrToken ??= await SignInWithPasswordlessAsync();

        // 3. No discoverable passkeys. We need an email address from the user to identify them.
        if (string.IsNullOrEmpty(emailOrToken))
        {
            LoginState = LoginStates.ReadyForLogin;
            yield return LoginState;
            yield break;
        }

        // 4. An email address has been supplied. Make this the username, and look it up in Passwordless.
        if (new EmailAddressAttribute().IsValid(emailOrToken))
        {
            LoginState = LoginStates.VerifyingEmail;
            user.ChangeUsername(emailOrToken);
            await Users.UpdateAsync((T)user);
            yield return LoginState;

            // 5. If the user has no passkeys, send them a magic link to log in.
            var hasPasskeys = await HasPasskeys(user.ExternalId);
            if (!hasPasskeys)
            {
                await SendMagicLinkAsync(user, $"{Nav.Uri}?token=$TOKEN");
                LoginState = LoginStates.AwaitingMagicLink;
                yield return LoginState;
                yield break;
            }

            // 6. If the user has passkeys, prompt them to sign in with one.
            emailOrToken = await SignInWithPasswordlessAsync(user);
        }

        // 7. The user has signed in with a passkey. Verify the token and log them in.
        LoginState = LoginStates.VerifyingToken;
        yield return LoginState;

        try
        {
            await LoginWithTokenAsync(emailOrToken);
            LoginState = LoginStates.LoggedIn;
        }
        catch (Exception e)
        {
            LoginState = LoginStates.Error;
            Message = e.Message;
        }

        yield return LoginState;
    }

    private async Task InitPasswordlessAsync()
    {
        var js = await Js.Value;
        await js.InvokeVoidAsync("init", publicKey);
    }

    private async Task<string> SignInWithPasswordlessAsync(BlossomUser? user = null)
    {
        var js = await Js.Value;
        return await js.InvokeAsync<string>("signInWithPasskey", user?.Username);
    }

    private async Task<string> SignUpWithPasswordlessAsync(BlossomUser user)
    {
        var js = await Js.Value;
        var registerToken = await PasswordlessClient.CreateRegisterTokenAsync(new RegisterOptions(user.Id, user.Username)
        {
            Aliases = [user.Username]
        });

        return await js.InvokeAsync<string>("signUpWithPasskey", registerToken.Token);
    }

    private async Task<bool> HasPasskeys(string? externalId)
    {
        if (externalId == null)
            return false;

        var credentials = await PasswordlessClient.ListCredentialsAsync(externalId);
        return credentials.Any();
    }

    public async Task<bool> SendMagicLinkAsync(BlossomUser user, string urlTemplate, int timeToLive = 3600)
        => await PostAsync("magic-links/send", new
        {
            emailAddress = user.Username,
            urlTemplate,
            userId = user.Id,
            timeToLive
        });

    private async Task<bool> PostAsync(string url, object payload)
    {
        var response = await Client.PostAsJsonAsync(url, payload);
        return response.IsSuccessStatusCode;
    }

    private async Task LoginWithTokenAsync(string token)
    {
        if (User == null)
            throw new Exception("User not initialized");

        var passwordlessUser = await PasswordlessClient.VerifyAuthenticationTokenAsync(token);
        if (passwordlessUser?.Success != true)
            throw new Exception("Unable to verify token");

        var hasPasskeys = await HasPasskeys(passwordlessUser.UserId);
        if (!hasPasskeys)
        {
            var result = await SignUpWithPasswordlessAsync(User);
        }

        var parentUser = Users.Query.FirstOrDefault(x => x.ExternalId == passwordlessUser.UserId && x.ParentUserId == null);
        if (parentUser != null)
        {
            User.SetParentUser(parentUser);
            await Users.UpdateAsync((T)User);
            User = parentUser;
        }
        else
        {
            User.Login("Passwordless", passwordlessUser.UserId);
        }

        await Users.UpdateAsync((T)User);
    }

    public override async IAsyncEnumerable<LoginStates> Logout(ClaimsPrincipal principal)
    {
        var user = await GetAsync(principal);

        user.Logout();

        await Users.UpdateAsync((T)User);

        yield return LoginStates.LoggedOut;
    }
}