using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Data;
using System.Security.Claims;

namespace Sparc.Blossom.Server.Authentication;

// Adapted from MS PersistingRevalidatingAuthenticationStateProvider
public class BlossomAuthenticationStateProvider<T> : RevalidatingServerAuthenticationStateProvider where T : BlossomUser
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PersistentComponentState _state;

    private readonly PersistingComponentStateSubscription _subscription;

    private Task<AuthenticationState>? _authenticationStateTask;

    public BlossomAuthenticationStateProvider(
        ILoggerFactory loggerFactory,
        IServiceScopeFactory scopeFactory,
        PersistentComponentState state)
        : base(loggerFactory)
    {
        _scopeFactory = scopeFactory;
        _state = state;

        AuthenticationStateChanged += OnAuthenticationStateChanged;
        _subscription = state.RegisterOnPersisting(OnPersistingAsync);
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

    private void OnAuthenticationStateChanged(Task<AuthenticationState> authenticationStateTask)
    {
        _authenticationStateTask = authenticationStateTask;
    }

    private async Task OnPersistingAsync()
    {
        if (_authenticationStateTask is null)
            return;

        var authenticationState = await _authenticationStateTask;
        var principal = authenticationState.User;

        if (principal.Identity?.IsAuthenticated == true)
            _state.PersistAsJson(nameof(BlossomUser), await GetAsync(principal));
    }

    protected override void Dispose(bool disposing)
    {
        _subscription.Dispose();
        AuthenticationStateChanged -= OnAuthenticationStateChanged;
        base.Dispose(disposing);
    }
}
