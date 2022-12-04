using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Sparc.Kernel;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

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
        AddClaim(ClaimTypes.NameIdentifier, Id);
        AddClaim(ClaimTypes.Name, UserName);
        RegisterClaims();

        var claims = Claims.Select(x => new Claim(x.Key, x.Value));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Sparc"));
    }
}
