using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public abstract class BlossomAuthenticator
{
    public abstract Task<BlossomUser?> LoginAsync(string userName, string password);
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

    public override async Task<BlossomUser?> LoginAsync(string userName, string password)
    {
        var result = await SignInManager.PasswordSignInAsync(userName, password, true, false);
        if (result.Succeeded)
            return await UserManager.FindByNameAsync(userName);

        return null;
    }

    public override async Task<BlossomUser?> RefreshClaimsAsync(ClaimsPrincipal principal)
    {
        return await UserManager.FindByIdAsync(principal.Id());
    }

    public async Task<string> CreateMagicSignInLinkAsync(string username, string returnUrl)
    {
        var user = await UserManager.FindByNameAsync(username);
        if (user == null)
        {
            user = new()
            {
                UserName = username
            };
            await UserManager.CreateAsync(user);
        }

        return await CreateMagicSignInLinkAsync(user, returnUrl);
    }

    public async Task<string> CreateMagicSignInLinkAsync(TUser user, string returnUrl)
    {
        await UserManager.UpdateSecurityStampAsync(user);

        var token = await UserManager.GenerateUserTokenAsync(user, "Default", "passwordless-auth");

        var url = "/_auth/login-silent";
        url = QueryHelpers.AddQueryString(url, "userId", user.Id);
        url = QueryHelpers.AddQueryString(url, "token", token);
        url = QueryHelpers.AddQueryString(url, "returnUrl", returnUrl);
        return url;
    }
}
