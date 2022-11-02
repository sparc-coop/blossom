using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Sparc.Authentication;

internal class PasswordlessJwtProvider
{
    internal PasswordlessJwtProvider(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public string CreateJwt(SparcUser user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenKey = Encoding.UTF8.GetBytes(Configuration["Passwordless:Key"]!);
        var identity = user.CreatePrincipal().Identity as ClaimsIdentity;
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = identity,
            Expires = DateTime.UtcNow.AddMinutes(60 * 24),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(tokenKey), SecurityAlgorithms.HmacSha256Signature)
        };
        var jwToken = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(jwToken);
    }
}
