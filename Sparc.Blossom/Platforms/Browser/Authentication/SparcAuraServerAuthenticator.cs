using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public class SparcAuraBrowserAuthenticator(ISparcAura aura, IRepository<BlossomUser> users) : IBlossomAuthenticator
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
        User = BlossomUser.FromPrincipal(principal);
        LoginState = LoginStates.LoggedIn;

        await LogoutAsync(principal);

        await users.AddAsync(User);

        return principal;
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

        var allUsers = users.Query.ToList();
        foreach (var user in allUsers)
            await users.DeleteAsync(user);

        return principal;
    }
}
