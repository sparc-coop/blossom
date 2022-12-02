using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Sparc.Kernel;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Sparc.Authentication;

public abstract class SparcUser : Root<string>, ISparcUser
{
    public SparcUser()
    { }
    
    public string? SecurityStamp { get; set; }

    public string? UserName { get; set; }

    public string? LoginProviderKey { get; set; }

    protected Dictionary<string, string> Claims { get; set; } = new();

    protected void AddClaim(string type, string? value)
    {
        if (value == null)
            return;
        
        if (Claims.ContainsKey(type))
            Claims[type] = value;
        else
            Claims.Add(type, value);
    }

    protected abstract void RegisterClaims();

    public virtual ClaimsPrincipal CreatePrincipal()
    {
        RegisterClaims();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Id)
        };

        foreach (var claim in Claims.Keys)
        {
            claims.Add(new Claim(claim, Claims[claim]));
        }

        if (!claims.Any(x => x.Type == ClaimTypes.Name) && UserName != null)
            claims.Add(new(ClaimTypes.Name, UserName));
        
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Sparc.Blossom"));
    }

    public virtual string CreateToken(JwtBearerOptions options, int expirationInMinutes = 60 * 24)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var identity = CreatePrincipal().Identity as ClaimsIdentity;

        var secretKey = options.TokenValidationParameters.IssuerSigningKeys.FirstOrDefault()
         ?? options.TokenValidationParameters.IssuerSigningKey;

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
