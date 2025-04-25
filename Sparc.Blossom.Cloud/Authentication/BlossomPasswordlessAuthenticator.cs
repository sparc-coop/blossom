using Microsoft.Extensions.Options;
using Passwordless;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public class BlossomPasswordlessAuthenticator<T> : BlossomDefaultAuthenticator<T>, IBlossomCloudApi
    where T : BlossomUser, new()
{
    IPasswordlessClient PasswordlessClient { get; }
    public HttpClient Client { get; }
    public BlossomPasswordlessAuthenticator(
        IPasswordlessClient _passwordlessClient,
        IOptions<PasswordlessOptions> options,
        IRepository<T> users)
        : base(users)
    {
        PasswordlessClient = _passwordlessClient;
        Client = new HttpClient
        {
            BaseAddress = new Uri("https://v4.passwordless.dev/")
        };
        Client.DefaultRequestHeaders.Add("ApiSecret", options.Value.ApiSecret);
    }

    public async Task<LoginStates> LoginWithPasswordless(ClaimsPrincipal principal, HttpContext context, string? emailOrToken = null)
    {
        Message = null;

        // 1. Convert the ClaimsPrincipal from the cookie into a BlossomUser
        // If the BlossomUser is already attached to Passwordless, they're logged in because their cookie is valid
        var user = await GetAsync(principal);

        if (user.ExternalId != null)
            return LoginStates.LoggedIn;
        else if (LoginState == LoginStates.NotInitialized && string.IsNullOrEmpty(emailOrToken))
            return LoginStates.LoggedOut;

        // 3. No discoverable passkeys. We need an email address from the user to identify them.
        if (string.IsNullOrEmpty(emailOrToken))
            return LoginStates.ReadyForLogin;

        // 4. An email address has been supplied. Make this the username, and look it up in Passwordless.
        if (new EmailAddressAttribute().IsValid(emailOrToken))
        {
            user.ChangeUsername(emailOrToken);
            await Users.UpdateAsync((T)user);

            // 5. If the user has no passkeys, send them a magic link to log in.
            var hasPasskeys = await HasPasskeys(user.ExternalId);
            if (!hasPasskeys)
            {
                await SendMagicLinkAsync(user, $"{context.Request.Headers.Referer}?token=$TOKEN");
                return LoginStates.AwaitingMagicLink;
            }
            else
            {
                return LoginStates.AwaitingPasskey;
            }
        }

        // 7. The user has signed in with a passkey. Verify the token and log them in.
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

        return LoginState;
    }

    private async Task<string> SignUpWithPasswordlessAsync(BlossomUser user)
    {
        var registerToken = await PasswordlessClient.CreateRegisterTokenAsync(new RegisterOptions(user.Id, user.Username)
        {
            Aliases = [user.Username]
        });

        return registerToken.Token;
    }

    private async Task<bool> HasPasskeys(string? externalId)
    {
        if (externalId == null)
            return false;

        var credentials = await PasswordlessClient.ListCredentialsAsync(externalId);
        return credentials.Any();
    }

    private async Task<bool> SendMagicLinkAsync(BlossomUser user, string urlTemplate, int timeToLive = 3600)
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

        await Users.UpdateAsync((T)User!);

        yield return LoginStates.LoggedOut;
    }

    public void Map(IEndpointRouteBuilder endpoints)
    {
        var auth = endpoints.MapGroup("/auth");
        auth.MapPost("login", LoginWithPasswordless);
        auth.MapGet("userinfo", GetAsync);
    }
}