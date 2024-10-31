﻿using Microsoft.AspNetCore.Components;
using Sparc.Blossom.Data;
using Sparc.Blossom.Server.Authentication;
using System.Diagnostics;
using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public class BlossomDefaultAuthenticator<T>
    (IRepository<T> users, ILoggerFactory loggerFactory, IServiceScopeFactory scopeFactory, PersistentComponentState state) 
    : BlossomAuthenticationStateProvider<T>(loggerFactory, scopeFactory, state), IBlossomAuthenticator 
    where T : BlossomUser, new()
{
    public LoginStates LoginState { get; set; } = LoginStates.LoggedOut;

    public BlossomUser? User { get; set; }
    public IRepository<T> Users { get; } = users;
    public string? Message { get; set; }

    public override async Task<BlossomUser> GetAsync(ClaimsPrincipal principal)
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

    public virtual async IAsyncEnumerable<LoginStates> Login(ClaimsPrincipal principal, string? emailOrToken = null)
    {
        LoginState = LoginStates.LoggedIn;
        yield return LoginState;
    }

    public virtual async IAsyncEnumerable<LoginStates> Logout(ClaimsPrincipal principal)
    {
        LoginState = LoginStates.LoggedOut;
        yield return LoginState;
    }

    private async Task<BlossomUser> GetUserAsync(ClaimsPrincipal principal)
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

}