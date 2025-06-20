using Microsoft.AspNetCore.Authentication;
using Sparc.Blossom.Authentication;
using System.Security.Claims;

namespace Sparc.Engine;

public class LanguageClaimsTransformation(KoriTranslator translator, IBlossomAuthenticator auth, IHttpContextAccessor http) 
    : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var user = await auth.GetAsync(principal);
        var accept = http.HttpContext?.Request.Headers.AcceptLanguage;

        if (principal.HasClaim(x => x.Type == "language") || !accept.HasValue)
            return principal;

        if (accept.HasValue && user.Avatar.Language == null)
        {
            translator.SetLanguage(user, accept);
            await auth.UpdateAsync(principal, user.Avatar);
        }
        
        principal.Identities.First().AddClaim(new Claim("language", user.Avatar.Language?.ToString() ?? accept!));

        return principal;
    }
}
