using System.Security.Claims;
using System.Threading.Tasks;

namespace Sparc.Core
{
    public interface ISparcAuthenticator
    {
        public Task<bool> LoginAsync();
        public Task<ClaimsPrincipal> LoginAsync(string returnUrl);
        public Task LogoutAsync();
        public ClaimsPrincipal User { get; set; }
    }
}