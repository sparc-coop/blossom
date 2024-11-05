using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public interface IAuthenticator
{
    public Task<bool> LoginAsync();
    public Task<ClaimsPrincipal> LoginAsync(string returnUrl);
    public Task LogoutAsync();
    public ClaimsPrincipal User { get; set; }
    LoginStates LoginState { get; set; }
    public string? Message { get; set; }
}