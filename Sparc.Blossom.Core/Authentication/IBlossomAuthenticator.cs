﻿using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public interface IBlossomAuthenticator
{
    LoginStates LoginState { get; set; }
    BlossomUser? User { get; }

    Task<BlossomUser?> GetAsync(ClaimsPrincipal principal);
    IAsyncEnumerable<LoginStates> LoginAsync(string? emailOrToken = null);
    IAsyncEnumerable<LoginStates> LogoutAsync();
}