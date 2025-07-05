using System.Security.Claims;
using Sparc.Blossom.Authentication;

namespace Sparc.Engine;

public class SparcEngineAuthenticator(ISparcEngine engine) : IBlossomAuthenticator
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
        var user = await engine.UpdateUserInfo(avatar);
        User = user;
        return user;
    }

    public async Task<ClaimsPrincipal> LoginAsync(ClaimsPrincipal principal)
    {
        if (User == null)
        {
            var user = await engine.Login();
            User = user;
        }

        LoginState = LoginStates.LoggedIn;
        return User.ToPrincipal();
    }

    public async Task<ClaimsPrincipal> LoginAsync(ClaimsPrincipal principal, string authenticationType, string externalId)
    {
        var user = await engine.Login(externalId);
        User = user;
        LoginState = LoginStates.LoggedIn;
        return user.ToPrincipal(authenticationType, externalId);
    }

    public Task<ClaimsPrincipal> LogoutAsync(ClaimsPrincipal principal)
    {
        User = null;
        LoginState = LoginStates.LoggedOut;
        return Task.FromResult(principal);
    }
}
