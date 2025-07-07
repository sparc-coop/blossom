using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public class BlossomDefaultAuthenticator<T>(IRepository<T> users) : IBlossomAuthenticator 
    where T : BlossomUser, new()
{
    public LoginStates LoginState { get; set; } = LoginStates.NotInitialized;

    public BlossomUser? User { get; set; }
    public string? Message { get; set; }
    protected IRepository<T> Users { get; } = users;

    public async Task<BlossomUser> GetAsync(ClaimsPrincipal principal)
    {
        return await GetUserAsync(principal);
    }

    public virtual async Task<ClaimsPrincipal> LoginAsync(ClaimsPrincipal principal)
    {
        var user = await GetUserAsync(principal);
        principal = user.ToPrincipal();
        await Users.UpdateAsync(user);
        return principal;
    }
    
    public virtual async Task<ClaimsPrincipal> LoginAsync(ClaimsPrincipal principal, string authenticationType, string externalId)
    {
        var user = await GetUserAsync(principal);
        principal = user.ToPrincipal(authenticationType, externalId);
        await Users.UpdateAsync(user);
        return principal;
    }

    public virtual async Task<ClaimsPrincipal> LogoutAsync(ClaimsPrincipal principal)
    {
        var user = await GetUserAsync(principal);
        principal = user.Logout();
        await Users.UpdateAsync(user);
        return principal;
    }

    protected virtual async Task<T> GetUserAsync(ClaimsPrincipal principal)
    {
        T? user = null;
        if (principal.Identity?.IsAuthenticated == true)
        {
            user = await Users.FindAsync(principal.Id());
        }

        if (user == null)
        {
            user = BlossomUser.FromPrincipal<T>(principal);
            await Users.AddAsync(user);
        }

        User = user;
        return user!;
    }

    public async Task<BlossomUser> UpdateAsync(ClaimsPrincipal principal, BlossomAvatar avatar)
    {
        var user = await GetUserAsync(principal);
        user.UpdateAvatar(avatar);
        await Users.UpdateAsync(user);
        return user;
    }

    public virtual Task<BlossomUser> LoginAsync(ClaimsPrincipal principal, string? emailOrToken = null)
    {
        throw new NotImplementedException();
    }
}
