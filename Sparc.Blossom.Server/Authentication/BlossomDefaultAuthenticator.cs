using Sparc.Blossom.Data;
using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public class BlossomDefaultAuthenticator<T>(IRepository<T> users) 
    : IBlossomAuthenticator where T : BlossomUser, new()
{
    public LoginStates LoginState { get; set; } = LoginStates.VerifyingToken;

    public BlossomUser? User { get; private set; }
    public IRepository<T> Users { get; } = users;

    public virtual async Task<BlossomUser?> GetAsync(ClaimsPrincipal principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            var user = new T() { Username = BlossomTools.FriendlyId() };
            await Users.AddAsync(user);
            User = user;
            return user;
        }

        User = await Users.FindAsync(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
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
