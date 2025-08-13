using Microsoft.IdentityModel.Tokens;
using Sparc.Blossom.Authentication;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace Sparc.Blossom.Authentication;

public class SparcTokens(IConfiguration config)
{
    internal static TokenValidationParameters DefaultParameters(IConfiguration config)
    {
        return new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!)),
            ValidateIssuer = true,
            ValidIssuer = config["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = config["Jwt:Audience"],
            ValidateLifetime = true
        };
    }
    
    internal BlossomLogin Create(BlossomUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var handler = new JwtSecurityTokenHandler();

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = user.ToIdentity(),
            Expires = DateTime.UtcNow.AddDays(90),
            SigningCredentials = credentials,
            Issuer = config["Jwt:Issuer"],
            Audience = config["Jwt:Audience"]
        };

        var token = handler.CreateToken(descriptor);
        var tokenStr = handler.WriteToken(token);
        return new(user, tokenStr);
    }

    internal BlossomUser FromToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var parameters = DefaultParameters(config);
        var principal = handler.ValidateToken(token, parameters, out var validatedToken);
        if (validatedToken is not JwtSecurityToken jwtToken)
            throw new SecurityTokenException("Invalid token");
        
        return BlossomUser.FromPrincipal(principal);
    }
}
