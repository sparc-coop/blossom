using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace Sparc.Authentication;

public static class UserManagerPasswordlessExtensions
{
    public static async Task<string> CreateMagicSignInLinkAsync<T>(this UserManager<T> manager, string username, string returnUrl) where T : SparcUser, new()
    {
        var user = await manager.FindByNameAsync(username);
        if (user == null)
        {
            user = new()
            {
                UserName = username
            };
            await manager.CreateAsync(user);
        }

        return await manager.CreateMagicSignInLinkAsync<T>(user, returnUrl);
    }

    public static async Task<string> CreateMagicSignInLinkAsync<T>(this UserManager<T> manager, T user, string returnUrl) where T : SparcUser, new()
    {
        var token = await manager.GenerateUserTokenAsync(user, "Default", "passwordless-auth");

        var url = "/PasswordlessLogin";
        url = QueryHelpers.AddQueryString(url, "userId", user.Id);
        url = QueryHelpers.AddQueryString(url, "token", token);
        url = QueryHelpers.AddQueryString(url, "returnUrl", returnUrl);
        return url;
    }
}
