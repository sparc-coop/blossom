using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System.Net.Mail;

namespace Sparc.Blossom.Authentication;

public class BlossomAuthenticator<TUser>(UserManager<TUser> UserManager, SignInManager<TUser> SignInManager, IHttpContextAccessor http) 
    : BlossomAuthenticator where TUser : BlossomUser, new()
{
    public override async Task<BlossomUser?> GetAsync()
    {
        var principal = http.HttpContext?.User ??
                throw new InvalidOperationException($"{nameof(GetAsync)} requires access to an {nameof(HttpContext)}.");

        var user = await UserManager.GetUserAsync(principal) ?? throw new NavigationException("/");
        return user;
    }
    
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

            user = new();
            user.Identity.UserName = username;
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
