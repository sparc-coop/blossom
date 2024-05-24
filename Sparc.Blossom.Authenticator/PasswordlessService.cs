using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Passwordless.Net;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Data;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Security.Claims;

namespace Sparc.Blossom.Authenticator;

public class PasswordlessService<T> : IPasswordlessService where T : BlossomUser, new()
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

    public PasswordlessService(
        IPasswordlessClient _passwordlessClient,
        IRepository<T> users,
        IOptions<PasswordlessOptions> options,
        AuthenticationStateProvider auth,
        NavigationManager nav,
        IJSRuntime js)
    //IHttpContextAccessor http)
    {
        PasswordlessClient = _passwordlessClient;
        Users = users;
        Auth = auth;
        Nav = nav;
        //Http = http;
        Js = new(() => js.InvokeAsync<IJSObjectReference>("import", "./LoginSignup.js").AsTask());

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
            User = await GetOrCreateUserAsync(emailOrToken) as T;
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

    public async Task<BlossomUser> GetOrCreateUserAsync(string username)
    {
        var js = await Js.Value;

        var user = await Users.Query.Where(x => x.UserName == username).FirstOrDefaultAsync();
        if (user == null)
        {
            user = new T() { UserName = username };
            await Users.AddAsync(user);
        }

        if (!await HasPasskeys(user))
        {
            await SendMagicLinkAsync(username, $"{Nav.Uri}?token=$TOKEN", user.LoginProviderKey);
            LoginState = LoginStates.AwaitingMagicLink;
        }

        return user;
    }

    private async Task<bool> HasPasskeys(BlossomUser user)
    {
        if (user.LoginProviderKey == null)
            return false;

        var credentials = await PasswordlessClient.ListCredentialsAsync(user.LoginProviderKey);
        return credentials.Any();
    }

    private async Task<string> GetOrCreatePasswordlessUserAsync(BlossomUser user)
    {
        var js = await Js.Value;

        try
        {
            return await HasPasskeys(user)
                ? await js.InvokeAsync<string>("signInWithPasskey", user.UserName)
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
        var registerToken = await PasswordlessClient.CreateRegisterTokenAsync(new RegisterOptions(user.LoginProviderKey, user.UserName)
        {
            Aliases = [user.UserName]
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

        var user = await Users.Query.Where(x => x.LoginProviderKey == verifiedUser.UserId).FirstAsync();
        return user;
    }

    private async Task<bool> PostAsync(string url, object payload)
    {
        var response = await Client.PostAsJsonAsync(url, payload);
        return response.IsSuccessStatusCode;
    }

}