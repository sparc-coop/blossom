using Sparc.Blossom.Authentication;

namespace Sparc.Blossom.Authenticator
{
    public interface IPasswordlessService
    {
        LoginStates LoginState { get; set; }
        BlossomUser? User { get; }

        Task<BlossomUser> GetOrCreateUserAsync(string username);
        IAsyncEnumerable<LoginStates> LoginAsync(string? emailOrToken = null);
        Task<BlossomUser?> LoginWithTokenAsync(string token);
        IAsyncEnumerable<LoginStates> LogoutAsync();
        Task<bool> SendMagicLinkAsync(string emailAddress, string urlTemplate, string userId, int timeToLive = 3600);
    }
}