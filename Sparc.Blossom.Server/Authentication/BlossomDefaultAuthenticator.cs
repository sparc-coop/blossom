using Microsoft.AspNetCore.Components;
using Sparc.Blossom.Data;
using Sparc.Blossom.Server.Authentication;
using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public class BlossomDefaultAuthenticator<T>
    (IRepository<T> users, ILoggerFactory loggerFactory, IServiceScopeFactory scopeFactory, PersistentComponentState state) 
    : BlossomAuthenticationStateProvider<T>(loggerFactory, scopeFactory, state), IBlossomAuthenticator 
    where T : BlossomUser, new()
{
    public LoginStates LoginState { get; set; } = LoginStates.VerifyingToken;

    public BlossomUser? User { get; private set; }
    public IRepository<T> Users { get; } = users;

    public override async Task<BlossomUser?> GetAsync(ClaimsPrincipal principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            var user = new T();
            await Users.AddAsync(user);
            User = user;
            LoginState = LoginStates.LoggedIn;
            return user;
        }

        User = await Users.FindAsync(principal.Id());
        if (User != null)
            LoginState = LoginStates.LoggedIn;
        return User;
    }

    public async IAsyncEnumerable<LoginStates> LoginAsync(string? emailOrToken = null)
    {
        LoginState = LoginStates.LoggedIn;
        yield return LoginState;
    }

    public IAsyncEnumerable<LoginStates> LogoutAsync()
    {
        throw new NotImplementedException();
    }
}
