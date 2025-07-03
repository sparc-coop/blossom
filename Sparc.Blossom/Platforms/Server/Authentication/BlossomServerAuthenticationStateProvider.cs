using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using System.Security.Claims;
using Sparc.Blossom.Authentication;

namespace Sparc.Blossom.Platforms.Server;

// Adapted from MS PersistingRevalidatingAuthenticationStateProvider
public class BlossomServerAuthenticationStateProvider<T>(ILoggerFactory loggerFactory, IServiceScopeFactory scopeFactory) 
    : RevalidatingServerAuthenticationStateProvider(loggerFactory) where T : BlossomUser
{
    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(30);

    protected override async Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState, CancellationToken cancellationToken)
    {
        return await GetAsync(authenticationState.User) != null;
    }

    public virtual async Task<BlossomUser> GetAsync(ClaimsPrincipal principal)
    {
        // Get the user from a new scope to ensure it fetches fresh data
        await using var scope = scopeFactory.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<IRepository<T>>();
        return await users.FindAsync(principal.Id()) ?? BlossomUser.FromPrincipal(principal);

    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }
}
