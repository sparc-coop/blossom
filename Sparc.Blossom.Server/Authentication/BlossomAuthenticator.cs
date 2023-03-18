using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using System.Net.Mail;
using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public abstract class BlossomAuthenticator
{
    public abstract Task<BlossomUser?> LoginAsync(string userName, string password, string? tokenProvider = null);
    public abstract Task<BlossomUser?> RefreshClaimsAsync(ClaimsPrincipal principal);
}

public class BlossomAuthenticator<TUser> : BlossomAuthenticator where TUser : BlossomUser, new()
{
    public BlossomAuthenticator(IConfiguration config, UserManager<TUser> userManager, SignInManager<TUser> signInManager)
    {
        Config = config;
        UserManager = userManager;
        SignInManager = signInManager;
    }

    public IConfiguration Config { get; }
    public UserManager<TUser> UserManager { get; }
    public SignInManager<TUser> SignInManager { get; }

    public override async Task<BlossomUser?> LoginAsync(string userName, string password, string? tokenProvider = null)
    {
        var success = tokenProvider switch
        {
            "Email" => await ValidateOneTimeCodeAsync(userName, password),
            "Phone" => await ValidateOneTimeCodeAsync(userName, password),
            "Link" => await ValidateMagicSignInLinkAsync(userName, password),
            _ => (await SignInManager.PasswordSignInAsync(userName, password, false, false)).Succeeded
        };

        if (success)
        {
            var user = await GetOrCreateAsync(userName);
            await SignInManager.SignInAsync(user, true);
            return user;
        }

        return null;
    }

    public override async Task<BlossomUser?> RefreshClaimsAsync(ClaimsPrincipal principal)
    {
        return await UserManager.FindByIdAsync(principal.Id());
    }

    public async Task<string> CreateOneTimeCodeAsync(string userName)
    {
        var tokenProvider = IsEmail(userName)
            ? TokenOptions.DefaultEmailProvider
            : TokenOptions.DefaultPhoneProvider;
        
        var user = await GetOrCreateAsync(userName);
        return await UserManager.GenerateTwoFactorTokenAsync(user, tokenProvider);
    }

    private async Task<bool> ValidateOneTimeCodeAsync(string userName, string password)
    {
        var tokenProvider = IsEmail(userName)
            ? TokenOptions.DefaultEmailProvider
            : TokenOptions.DefaultPhoneProvider;

        var user = await GetOrCreateAsync(userName);
        
        return await UserManager.VerifyTwoFactorTokenAsync(user, tokenProvider, password);
    }

    public async Task<string> CreateMagicSignInLinkAsync(string username, string returnUrl, HttpRequest? request = null)
    {
        var user = await GetOrCreateAsync(username);
        var token = await UserManager.GenerateUserTokenAsync(user, "Default", "passwordless-auth");

        var url = "/_auth/login-silent";
        url = QueryHelpers.AddQueryString(url, "userId", user.Id);
        url = QueryHelpers.AddQueryString(url, "token", token);
        url = QueryHelpers.AddQueryString(url, "returnUrl", returnUrl);

        if (request != null)
            url = $"{request.Scheme}://{request.Host.Value.TrimEnd('/')}/{url.TrimStart('/')}";

        return url;
    }

    public async Task<bool> ValidateMagicSignInLinkAsync(string username, string token)
    {
        var user = await GetOrCreateAsync(username);
        if (user == null)
            return false;
        
        return await UserManager.VerifyUserTokenAsync(user, "Default", "passwordless-auth", token);
    }

    private async Task<TUser> GetOrCreateAsync(string username)
    {
        var user = Guid.TryParse(username, out Guid id)
            ? await UserManager.FindByIdAsync(username)
            : await UserManager.FindByNameAsync(username);

        if (user == null)
        {
            if (id != Guid.Empty)
                throw new ArgumentException($"User ID {id} not found.");

            user = new()
            {
                UserName = username
            };
            await UserManager.CreateAsync(user);
        }

        return user;
    }

    private static bool IsEmail(string address)
    {
        try
        {
            var m = new MailAddress(address);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
