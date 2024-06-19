using Microsoft.AspNetCore.Components.Authorization;
using Sparc.Blossom.Data;
using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public class BlossomDeviceAuthenticator<T>(IDevice device, IRepository<T> users, AuthenticationStateProvider auth) 
    : IBlossomAuthenticator where T : BlossomUser, new()
{
    public LoginStates LoginState { get; set; } = LoginStates.VerifyingToken;

    public BlossomUser? User { get; private set; }
    public IDevice Device { get; } = device;
    public IRepository<T> Users { get; } = users;
    public AuthenticationStateProvider Auth { get; } = auth;

    public async Task<BlossomUser?> GetAsync(ClaimsPrincipal principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
            return null;

        return await Users.FindAsync(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    public async Task<BlossomUser> GetAsync(string username)
    {
        var user = Users.Query.FirstOrDefault(x => x.Username == username);
        if (user == null)
        {
            user = new T() 
            { 
                Username = username,
                AuthenticationType = "Device",
                ExternalId = Device.Id
            };

            await Users.AddAsync(user);
        }

        return user;
    }

    public async IAsyncEnumerable<LoginStates> LoginAsync(string? emailOrToken = null)
    {
        emailOrToken ??= Device.Id;
        LoginState = LoginStates.VerifyingToken;
        yield return LoginState;

        User = await LoginWithTokenAsync(emailOrToken!) as T;
        if (User == null)
        {
            LoginState = LoginStates.LoggedOut;
            yield return LoginState;
            yield break;
        }

        LoginState = LoginStates.LoggedIn;
        yield return LoginState;
    }

    public async Task<BlossomUser?> LoginWithTokenAsync(string token)
    {
        var user = Users.Query.First(x => x.ExternalId == token);
        return user;
    }

    public IAsyncEnumerable<LoginStates> LogoutAsync()
    {
        throw new NotImplementedException();
    }

    public Task<bool> SendMagicLinkAsync(string emailAddress, string urlTemplate, string userId, int timeToLive = 3600)
    {
        throw new NotImplementedException();
    }
}
