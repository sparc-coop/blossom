using System.Security.Claims;
using Sparc.Blossom.Authentication;

namespace Sparc.Engine;

public class SparcEngineAuthenticator : IBlossomAuthenticator
{
    private readonly ISparcEngine _engine;
    public LoginStates LoginState { get; set; } = LoginStates.NotInitialized;
    public BlossomUser? User { get; private set; }
    public string? Message { get; set; }

    public SparcEngineAuthenticator(ISparcEngine engine)
    {
        _engine = engine;
    }

    public async Task<BlossomUser> GetAsync(ClaimsPrincipal principal)
    {
        // Try to get user info from the engine
        try
        {
            var user = await _engine.UserInfo();
            User = user;
            return user;
        }
        catch
        {
            return new BlossomUser();
        }
    }

    public async Task<BlossomUser> UpdateAsync(ClaimsPrincipal principal, BlossomAvatar avatar)
    {
        var user = await _engine.UpdateUserInfo(avatar);
        User = user;
        return user;
    }

    public async Task<ClaimsPrincipal> LoginAsync(ClaimsPrincipal principal)
    {
        if (User == null)
        {
            var user = await _engine.Login();
            User = user;
        }

        LoginState = LoginStates.LoggedIn;
        return User.ToPrincipal();
    }

    public async Task<ClaimsPrincipal> LoginAsync(ClaimsPrincipal principal, string authenticationType, string externalId)
    {
        var user = await _engine.Login(externalId);
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
