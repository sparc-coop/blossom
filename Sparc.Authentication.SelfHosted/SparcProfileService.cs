using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Extensions;
using System.Threading.Tasks;

namespace Sparc.Authentication.SelfHosted
{
    public class SparcProfileService : IProfileService
    {
        public SparcProfileService(SparcAuthenticator authenticator)
        {
            Authenticator = authenticator;
        }

        public SparcAuthenticator Authenticator { get; }

        public async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            var id = context.Subject.GetSubjectId();

            var claims = await Authenticator.GetClaimsAsync(id);

            context.IssuedClaims = claims;
        }

        public async Task IsActiveAsync(IsActiveContext context)
        {
            var id = context.Subject.GetSubjectId();
            context.IsActive = await Authenticator.IsActiveAsync(id);
        }
    }
}