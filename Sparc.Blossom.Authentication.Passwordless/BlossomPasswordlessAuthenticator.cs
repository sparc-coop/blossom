using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Passwordless.Net;
using Sparc.Blossom.Data;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Net.Http.Json;

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
        ILoggerFactory loggerFactory,
        IServiceScopeFactory scopeFactory,
        PersistentComponentState state,
        NavigationManager nav,
        IJSRuntime js)
        : base(users, loggerFactory, scopeFactory, state)
    {
        PasswordlessClient = _passwordlessClient;
        Js = new(() => js.InvokeAsync<IJSObjectReference>("import", "./_content/Sparc.Blossom.Passwordless/LoginSignup.js").AsTask());
        Client = new HttpClient
        {
            BaseAddress = new Uri("https://v4.passwordless.dev/")
        };
        Client.DefaultRequestHeaders.Add("ApiSecret", options.Value.ApiSecret);
        Nav = nav;

        publicKey = options.Value.ApiKey!;
    }

    public override async Task<BlossomUser?> GetAsync(ClaimsPrincipal principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            var sessionUser = new T();
            await Users.AddAsync(sessionUser);
            User = sessionUser;
            return sessionUser;
        }

        User = await Users.FindAsync(principal.Id());
        return User;
    }

    public override async IAsyncEnumerable<LoginStates> LoginAsync(ClaimsPrincipal? principal, string? emailOrToken = null)
    {
        var js = await Js.Value;
        await js.InvokeVoidAsync("init", publicKey);
        var isMagicLinkReturn = emailOrToken?.StartsWith("verify") == true;

        emailOrToken ??= await js.InvokeAsync<string>("signInWithPasskey", null);

        if (string.IsNullOrEmpty(emailOrToken))
        {
            LoginState = LoginStates.ReadyForLogin;
            yield return LoginState;
            yield break;
        }

        if (!string.IsNullOrEmpty(emailOrToken) && new EmailAddressAttribute().IsValid(emailOrToken))
        {
            LoginState = LoginStates.VerifyingEmail;
            User = await GetOrCreateUserAsync(emailOrToken) as T;
            if (LoginState == LoginStates.AwaitingMagicLink)
            {
                yield return LoginState;
                yield break;
            }

            emailOrToken = await GetOrCreatePasswordlessUserAsync(User!);
        }

        LoginState = LoginStates.VerifyingToken;
        User = await LoginWithTokenAsync(emailOrToken, principal!) as T;

        if (User != null)
        {
            if (isMagicLinkReturn)
            {
                await GetOrCreatePasswordlessUserAsync(User);
            }
            LoginState = LoginStates.LoggedIn;
            yield return LoginState;
        }
        else
        {
            //SetError("Couldn't log in!");
            LoginState = LoginStates.Error;
            yield return LoginState;
        }
    }

    public async Task<BlossomUser> GetOrCreateUserAsync(string username)
    {
        var js = await Js.Value;

        var user = await Users.Query.Where(x => x.Username == username).FirstOrDefaultAsync();
        if (user == null)
        {
            user = new T() { Username = username, ExternalId = Guid.NewGuid().ToString() };
            await Users.AddAsync(user);
        }

        if (!await HasPasskeys(user))
        {
            await SendMagicLinkAsync(username, $"{Nav.Uri}?token=$TOKEN", user.ExternalId!);
            LoginState = LoginStates.AwaitingMagicLink;
        }

        return user;
    }
    private async Task<string> GetOrCreatePasswordlessUserAsync(BlossomUser user)
    {
        var js = await Js.Value;

        try
        {
            return await HasPasskeys(user)
                ? await js.InvokeAsync<string>("signInWithPasskey", user.Username)
                : await SignUpWithPasskeyAsync(user);
        }
        catch
        {
            return await SignUpWithPasskeyAsync(user);
        }
    }
    private async Task<string> SignUpWithPasskeyAsync(BlossomUser user)
    {
        var js = await Js.Value;
        var registerToken = await PasswordlessClient.CreateRegisterTokenAsync(new RegisterOptions(user.ExternalId!, user.Username)
        {
            Aliases = [user.Username]
        });

        return await js.InvokeAsync<string>("signUpWithPasskey", registerToken.Token);
    }
    private async Task<bool> HasPasskeys(BlossomUser user)
    {
        if (user.ExternalId == null)
            return false;

        var credentials = await PasswordlessClient.ListCredentialsAsync(user.ExternalId);
        return credentials.Any();
    }
    public async Task<bool> SendMagicLinkAsync(string emailAddress, string urlTemplate, string userId, int timeToLive = 3600)
        => await PostAsync("magic-links/send", new
        {
            emailAddress,
            urlTemplate,
            userId,
            timeToLive
        });
    private async Task<bool> PostAsync(string url, object payload)
    {
        var response = await Client.PostAsJsonAsync(url, payload);
        return response.IsSuccessStatusCode;
    }
    public async Task<BlossomUser?> LoginWithTokenAsync(string token, ClaimsPrincipal principal)
    {
        var testete = await GetAsync(principal);

        var verifiedUser = await PasswordlessClient.VerifyTokenAsync(token);
        if (verifiedUser?.Success != true)
            throw new Exception("Unable to verify token");

        var parentUser = await Users.Query.Where(x => x.ExternalId == verifiedUser.UserId && x.ParentUserId == null).FirstAsync();

        User!.Username = parentUser.Username;
        User.ParentUserId = parentUser.Id;
        User.ExternalId = parentUser.ExternalId;

        await Users.UpdateAsync((T)User);

        return parentUser;
    }
}
