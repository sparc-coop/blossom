using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public class BlossomDefaultAuthenticator<T>(IRepository<T> users) : AuthenticationStateProvider, IBlossomAuthenticator 
    where T : BlossomUser, new()
{
    public LoginStates LoginState { get; set; } = LoginStates.NotInitialized;

    public BlossomUser? User { get; set; }
    public IRepository<T> Users { get; } = users;
    public string? Message { get; set; }

    public async Task<BlossomUser> GetAsync(ClaimsPrincipal principal)
    {
        return await GetUserAsync(principal);
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        User = Users.Query.FirstOrDefault();
        if (User == null)
        {
            User = new BlossomUser();
            await Users.AddAsync((T)User);
        }

        var principal = User.ToPrincipal();
        var state = new AuthenticationState(principal);

        return state;
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public virtual async IAsyncEnumerable<LoginStates> Login(ClaimsPrincipal principal, string authenticationType, string externalId)
    {
        LoginState = LoginStates.LoggedIn;
        yield return LoginState;
    }

    public virtual async IAsyncEnumerable<LoginStates> Logout(ClaimsPrincipal principal)
    {
        LoginState = LoginStates.LoggedOut;
        yield return LoginState;
    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

    public virtual async Task<ClaimsPrincipal> LoginAsync(ClaimsPrincipal principal)
    {
        var user = await GetAsync(principal);
        principal = user.ToPrincipal();
        await Users.UpdateAsync((T)user);
        return principal;
    }
    
    public virtual async Task<ClaimsPrincipal> LoginAsync(ClaimsPrincipal principal, string authenticationType, string externalId)
    {
        var user = await GetUserAsync(principal);
        principal = user.ToPrincipal(authenticationType, externalId);
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

    protected virtual async Task<BlossomUser> GetUserAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated == true)
        {
            User = await Users.FindAsync(principal.Id());
        }

        if (User == null)
        {
            User = BlossomUser.FromPrincipal(principal);
            await Users.AddAsync((T)User);
        }

        return User!;
    }

    public async Task<BlossomUser> UpdateAsync(ClaimsPrincipal principal, BlossomAvatar avatar)
    {
        var user = await GetUserAsync(principal);
        user.UpdateAvatar(avatar);
        await Users.UpdateAsync((T)user);
        return user;
    }

    public virtual Task<BlossomUser> LoginAsync(ClaimsPrincipal principal, string? emailOrToken = null)
    {
        throw new NotImplementedException();
    }
}
