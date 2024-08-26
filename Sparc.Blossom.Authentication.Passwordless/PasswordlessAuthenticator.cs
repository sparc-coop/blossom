using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Passwordless;
using Sparc.Blossom.Data;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public class PasswordlessAuthenticator<T> : IBlossomAuthenticator where T : BlossomUser, new()
{
    IPasswordlessClient PasswordlessClient { get; }
    IRepository<T> Users { get; }
    public AuthenticationStateProvider Auth { get; }
    public NavigationManager Nav { get; }
    //public IHttpContextAccessor Http { get; }
    public HttpClient Client { get; }

    public LoginStates LoginState { get; set; } = LoginStates.LoggedOut;
    public string? ErrorMessage { get; private set; }
    readonly Lazy<Task<IJSObjectReference>> Js;
    public BlossomUser? User { get; private set; }

    readonly string publicKey;

    public PasswordlessAuthenticator(
        IPasswordlessClient _passwordlessClient,
        IRepository<T> users,
        IOptions<PasswordlessOptions> options,
        AuthenticationStateProvider auth,
        NavigationManager nav,
        IJSRuntime js)
    {
        PasswordlessClient = _passwordlessClient;
        Users = users;
        Auth = auth;
        Nav = nav;
        //Http = http;
        Js = new(() => js.InvokeAsync<IJSObjectReference>("import", "./PasswordlessAuthenticator.js").AsTask());

        Client = new HttpClient
        {
            BaseAddress = new Uri("https://v4.passwordless.dev/")
        };
        Client.DefaultRequestHeaders.Add("ApiSecret", options.Value.ApiSecret);

        publicKey = options.Value.ApiKey!;
    }

    public async IAsyncEnumerable<LoginStates> LoginAsync(string? emailOrToken = null)
    {
        var js = await Js.Value;
        await js.InvokeVoidAsync("init", publicKey);
        var isMagicLinkReturn = emailOrToken?.StartsWith("verify") == true;

        // Autofill
        emailOrToken ??= await js.InvokeAsync<string>("signInWithPasskey", null);

        if (new EmailAddressAttribute().IsValid(emailOrToken))
        {
            LoginState = LoginStates.VerifyingEmail;
            yield return LoginState;

            // Email login
            User = await GetAsync(emailOrToken) as T;
            yield return LoginState;

            if (LoginState == LoginStates.AwaitingMagicLink)
                yield break;

            emailOrToken = await GetOrCreatePasswordlessUserAsync(User);
        }

        LoginState = LoginStates.VerifyingToken;
        yield return LoginState;

        User = await LoginWithTokenAsync(emailOrToken) as T;

        if (User != null)
        {
            if (isMagicLinkReturn)
                await GetOrCreatePasswordlessUserAsync(User);

            LoginState = LoginStates.LoggedIn;
            yield return LoginState;
        }
        else
        {
            SetError("Couldn't log in!");
            yield return LoginState;
        }
    }

    public async IAsyncEnumerable<LoginStates> LogoutAsync()
    {
        LoginState = LoginStates.LoggingOut;
        yield return LoginState;

        User = null;
        LoginState = LoginStates.LoggedOut;
        yield return LoginState;
    }


    private void SetError(string error)
    {
        ErrorMessage = error;
        LoginState = LoginStates.Error;
    }

    public async Task<BlossomUser?> GetAsync(ClaimsPrincipal principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
            return null;

        return await GetAsync(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    public async Task<BlossomUser> GetAsync(string username)
    {
        var js = await Js.Value;

        var user = Users.Query.FirstOrDefault(x => x.Username == username);
        if (user == null)
        {
            user = new T() { Username = username };
            await Users.AddAsync(user);
        }

        if (!await HasPasskeys(user))
        {
            await SendMagicLinkAsync(username, $"{Nav.Uri}?token=$TOKEN", user.ExternalId);
            LoginState = LoginStates.AwaitingMagicLink;
        }

        return user;
    }

    private async Task<bool> HasPasskeys(BlossomUser user)
    {
        var credentials = await PasswordlessClient.ListCredentialsAsync(user.ExternalId);
        return credentials.Any();
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
        var registerToken = await PasswordlessClient.CreateRegisterTokenAsync(new RegisterOptions(user.ExternalId, user.Username)
        {
            Aliases = [user.Username]
        });

        return await js.InvokeAsync<string>("signUpWithPasskey", registerToken.Token);
    }

    public async Task<bool> SendMagicLinkAsync(string emailAddress, string urlTemplate, string userId, int timeToLive = 3600)
        => await PostAsync("magic-links/send", new
        {
            emailAddress,
            urlTemplate,
            userId,
            timeToLive
        });

    public async Task<BlossomUser?> LoginWithTokenAsync(string token)
    {
        var verifiedUser = await PasswordlessClient.VerifyTokenAsync(token);
        if (verifiedUser?.Success != true)
            throw new Exception("Unable to verify token");

        var claims = new List<Claim>
        {
            new(ClaimTypes.Sid, verifiedUser.UserId)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        //if (Http.HttpContext != null)
        //    await Http.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);

        var user = Users.Query.First(x => x.ExternalId == verifiedUser.UserId);
        return user;
    }

    private async Task<bool> PostAsync(string url, object payload)
    {
        var response = await Client.PostAsJsonAsync(url, payload);
        return response.IsSuccessStatusCode;
    }

    public IAsyncEnumerable<LoginStates> LoginAsync(ClaimsPrincipal? principal, string? emailOrToken = null)
    {
        return LoginAsync(emailOrToken);
    }

    public IAsyncEnumerable<LoginStates> LogoutAsync(ClaimsPrincipal? principal)
    {
        return LogoutAsync();
    }
}