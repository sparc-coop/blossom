using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Passwordless.Net;
using Shape.Schemas;
using Sparc.Blossom.Data;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Security.Claims;

namespace Sparc.Blossom.Authenticator;

public class PasswordlessService
{
    IPasswordlessClient PasswordlessClient { get; }
    IRepository<User> Users { get; }
    IRepository<UserRole> UserRoles { get; }
    public AuthenticationStateProvider Auth { get; }
    public NavigationManager Nav { get; }
    //public IHttpContextAccessor Http { get; }
    public HttpClient Client { get; }

    public LoginStates LoginState { get; set; } = LoginStates.LoggedOut;
    public string? ErrorMessage { get; private set; }
    readonly Lazy<Task<IJSObjectReference>> Js;
    public User? User { get; private set; }

    readonly string publicKey;

    public PasswordlessService(
        IPasswordlessClient _passwordlessClient,
        IRepository<User> users,
        IRepository<UserRole> userRole,
        IOptions<PasswordlessOptions> options,
        AuthenticationStateProvider auth,
        NavigationManager nav,
        IJSRuntime js)
        //IHttpContextAccessor http)
    {
        PasswordlessClient = _passwordlessClient;
        Users = users;
        UserRoles = userRole;
        Auth = auth;
        Nav = nav;
        //Http = http;
        Js = new(() => js.InvokeAsync<IJSObjectReference>("import", "./Auth/LoginSignup.razor.js").AsTask());

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
            User = await GetOrCreateUserAsync(emailOrToken);
            yield return LoginState;

            if (LoginState == LoginStates.AwaitingMagicLink)
                yield break;

            emailOrToken = await GetOrCreatePasswordlessUserAsync(User);
        }

        LoginState = LoginStates.VerifyingToken;
        yield return LoginState;

        User = await LoginWithTokenAsync(emailOrToken);

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

    public async Task<User> GetOrCreateUserAsync(string email)
    {
        var js = await Js.Value;

        var user = await Users.Query.Where(x => x.Email == email).FirstOrDefaultAsync();
        if (user == null)
        {
            user = new User(email);
            await Users.AddAsync(user);
        }

        if (!user.IsActive)
        {
            await SendMagicLinkAsync(email, $"{Nav.Uri}?token=$TOKEN", user.ExternalId);
            LoginState = LoginStates.AwaitingMagicLink;
        }

        return user;
    }

    private async Task<string> GetOrCreatePasswordlessUserAsync(User user)
    {
        var js = await Js.Value;

        try
        {
            var credentials = await PasswordlessClient.ListCredentialsAsync(user.ExternalId);
            return credentials.Any()
                ? await js.InvokeAsync<string>("signInWithPasskey", user.Email)
                : await SignUpWithPasskeyAsync(user);
        }
        catch
        {
            return await SignUpWithPasskeyAsync(user);
        }
    }

    private async Task<string> SignUpWithPasskeyAsync(User user)
    {
        var js = await Js.Value;
        var registerToken = await PasswordlessClient.CreateRegisterTokenAsync(new RegisterOptions(user.ExternalId, user.Email)
        {
            Aliases = [user.Email]
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

    public async Task<User?> LoginWithTokenAsync(string token)
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

        var user = await Users.Query.Where(x => x.ExternalId == verifiedUser.UserId).FirstAsync();
        if (!user.IsActive)
        {
            user.IsActive = true;
            await Users.UpdateAsync(user);
        }
        return user;
    }

    private async Task<bool> PostAsync(string url, object payload)
    {
        var response = await Client.PostAsJsonAsync(url, payload);
        return response.IsSuccessStatusCode;
    }

}