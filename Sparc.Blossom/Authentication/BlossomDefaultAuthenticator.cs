using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public class BlossomDefaultAuthenticator<T>(IRepository<T> users) : IBlossomAuthenticator<T> 
    where T : BlossomUser, new()
{
    public LoginStates LoginState { get; set; } = LoginStates.NotInitialized;

    public T? User { get; set; }
    public BlossomUser? Principal { get; set; }
    public IRepository<T> Users { get; } = users;
    public string? Message { get; set; }

    public async Task<BlossomUser> GetAsync(ClaimsPrincipal principal)
    {
        return await GetUserAsync(principal);
    }

    public virtual async Task<ClaimsPrincipal> LoginAsync(ClaimsPrincipal principal)
    {
        var user = await GetAsync(principal);
        principal = user.Login();
        await Users.UpdateAsync((T)user);
        return principal;
    }
    
    public virtual async Task<ClaimsPrincipal> LoginAsync(ClaimsPrincipal principal, string authenticationType, string externalId)
    {
        var user = await GetUserAsync(principal);
        principal = user.Login(authenticationType, externalId);
        await Users.UpdateAsync((T)user);
        return principal;
    }

    public virtual async Task<ClaimsPrincipal> LogoutAsync(ClaimsPrincipal principal)
    {
        var user = await GetUserAsync(principal);
        principal = user.Logout();
        await Users.UpdateAsync((T)user);
        return principal;
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public virtual async IAsyncEnumerable<LoginStates> Login(ClaimsPrincipal? principal, string? emailOrToken = null)
    {
        LoginState = LoginStates.LoggedIn;
        yield return LoginState;
    }

    public virtual async IAsyncEnumerable<LoginStates> Logout(ClaimsPrincipal? principal)
    {
        LoginState = LoginStates.LoggedOut;
        yield return LoginState;
    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

    private async Task<T> GetUserAsync(ClaimsPrincipal principal)
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
        Principal = user;

        return User!;
    }

}
