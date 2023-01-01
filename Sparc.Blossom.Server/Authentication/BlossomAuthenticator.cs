using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Sparc.Blossom.Authentication;

public abstract class BlossomAuthenticator
{
    public BlossomAuthenticator(IConfiguration config)
    {
        Config = config;
    }

    public IConfiguration Config { get; }

    public abstract Task<BlossomUser?> LoginAsync(string userName, string password);
    public abstract Task<BlossomUser?> LoginAsync(ClaimsPrincipal principal);
    
    public virtual string CreateToken(ClaimsPrincipal principal, string? signingKey = null, int expirationInMinutes = 60 * 24)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var identity = principal.Identity as ClaimsIdentity;

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

    public virtual string CreateToken(BlossomUser user, string? signingKey = null, int expirationInMinutes = 60 * 24)
        => CreateToken(user.CreatePrincipal(), signingKey, expirationInMinutes);
}
