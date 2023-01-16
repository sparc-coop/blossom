using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public class PasswordlessAuthenticator<T> : BlossomAuthenticator where T : BlossomUser, new()
{
    public PasswordlessAuthenticator(IConfiguration config, UserManager<T> userManager) : base(config)
    {
        UserManager = userManager;
    }

    public UserManager<T> UserManager { get; }

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

    public async Task<string> CreateMagicSignInLinkAsync(T user, string returnUrl)
    {
        if (user.SecurityStamp == null)
            await UserManager.UpdateSecurityStampAsync(user);
        
        var token = await UserManager.GenerateUserTokenAsync(user, "Default", "passwordless-auth");

        var url = "/_authenticate";
        url = QueryHelpers.AddQueryString(url, "userId", user.Id);
        url = QueryHelpers.AddQueryString(url, "token", token);
        url = QueryHelpers.AddQueryString(url, "returnUrl", returnUrl);
        return url;
    }

    public override Task<BlossomUser?> LoginAsync(string userName, string? password = null)
    {
        throw new NotImplementedException();
    }

    public override Task<BlossomUser?> RefreshClaimsAsync(ClaimsPrincipal principal)
    {
        throw new NotImplementedException();
    }
}
