using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using System.Security.Claims;
using Sparc.Blossom.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Sparc.Blossom.Platforms.Server;

// Adapted from MS PersistingRevalidatingAuthenticationStateProvider
public class BlossomServerAuthenticationStateProvider<T> : RevalidatingServerAuthenticationStateProvider where T : BlossomUser
{
    private readonly IServiceScopeFactory _scopeFactory;

    public BlossomServerAuthenticationStateProvider(ILoggerFactory loggerFactory, IServiceScopeFactory scopeFactory)
        : base(loggerFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(30);

    protected override async Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState, CancellationToken cancellationToken)
    {
        return await GetAsync(authenticationState.User) != null;
    }

    public virtual async Task<BlossomUser> GetAsync(ClaimsPrincipal principal)
    {
        // Get the user from a new scope to ensure it fetches fresh data
        await using var scope = _scopeFactory.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<IRepository<T>>();
        return await users.FindAsync(principal.Id()) ?? BlossomUser.FromPrincipal(principal);

    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }
}
