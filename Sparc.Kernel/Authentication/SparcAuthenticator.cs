using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Sparc.Authentication;

public abstract class SparcAuthenticator
{
    public SparcAuthenticator(IConfiguration config)
    {
        Config = config;
    }

    public IConfiguration Config { get; }

    public abstract Task<SparcUser?> LoginAsync(string userName, string password);
    
    public virtual string CreateToken(SparcUser user, string? signingKey = null, int expirationInMinutes = 60 * 24)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var identity = user.CreatePrincipal().Identity as ClaimsIdentity;

        var secretKey = Encoding.UTF8.GetBytes(signingKey ?? Config["Jwt:Key"]!);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = identity,
            Expires = DateTime.UtcNow.AddMinutes(expirationInMinutes),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(secretKey), SecurityAlgorithms.HmacSha256Signature)
        };
        var jwToken = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(jwToken);
    }
}
