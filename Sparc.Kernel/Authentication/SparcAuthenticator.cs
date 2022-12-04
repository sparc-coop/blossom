using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Sparc.Authentication;

public abstract class SparcAuthenticator
{
    public SparcAuthenticator(IOptionsSnapshot<JwtBearerOptions> config)
    {
        Config = config;
    }

    public IOptionsSnapshot<JwtBearerOptions> Config { get; }

    public abstract Task<SparcUser?> LoginAsync(string userName, string password);
    
    public virtual string CreateToken(SparcUser user, int expirationInMinutes = 60 * 24)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var identity = user.CreatePrincipal().Identity as ClaimsIdentity;

        var secretKey = Config.Value.TokenValidationParameters.IssuerSigningKeys.FirstOrDefault()
         ?? Config.Value.TokenValidationParameters.IssuerSigningKey;

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = identity,
            Expires = DateTime.UtcNow.AddMinutes(expirationInMinutes),
            SigningCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256Signature)
        };
        var jwToken = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(jwToken);
    }
}
