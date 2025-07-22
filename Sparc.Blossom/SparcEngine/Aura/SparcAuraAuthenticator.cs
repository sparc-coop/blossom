using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Sparc.Blossom.Authentication;
using System.Security.Claims;

namespace Sparc.Engine.Aura;

public class SparcAuraAuthenticator(ISparcAura aura, IHttpContextAccessor http) : IBlossomAuthenticator
{
    public LoginStates LoginState { get; set; } = LoginStates.NotInitialized;
    public BlossomUser? User { get; private set; }
    public string? Message { get; set; }

    public async Task<BlossomUser> GetAsync(ClaimsPrincipal principal)
    {
        if (User == null)
            await LoginAsync(principal);

        return User!;
    }

    public async Task<BlossomUser> UpdateAsync(ClaimsPrincipal principal, BlossomAvatar avatar)
    {
        var user = await aura.UpdateUserInfo(avatar);
        User = user;
        return user;
    }

    public async Task<ClaimsPrincipal> RegisterAsync()
    {
        var user = await aura.Login();
        User = user.ToUser();
        return await LoginAsync(User.ToPrincipal());
    }

    public async Task<ClaimsPrincipal> LoginAsync(ClaimsPrincipal principal)
    {
        if (User == null)
        {
            var user = await aura.Login();
            User = user.ToUser();
        }

        LoginState = LoginStates.LoggedIn;

        var newPrincipal = User.ToPrincipal();
        if (http.HttpContext != null)
        {
            http.HttpContext.User = newPrincipal;
            await http.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, http.HttpContext.User, new() { IsPersistent = true });
        }

        return newPrincipal;
    }

    public async Task<ClaimsPrincipal> LoginAsync(ClaimsPrincipal principal, string authenticationType, string externalId)
    {
        var user = await aura.Login(externalId);
        User = user.ToUser();
        LoginState = LoginStates.LoggedIn;
        return User.ToPrincipal(authenticationType, externalId);
    }

    public async Task<ClaimsPrincipal> LogoutAsync(ClaimsPrincipal principal)
    {
        User = null;
        LoginState = LoginStates.LoggedOut;

        if (http.HttpContext != null)
            await http.HttpContext.SignOutAsync();

        return principal;
    }
}
