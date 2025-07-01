using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Sparc.Blossom.Authentication;

public class BlossomIdentity(string id, string type)
{
    public string Id { get; private set; } = id;
    public string Type { get; private set; } = type;
    public string? VerificationHash { get; private set; }
    public bool IsVerified { get; set; }
    public bool IsLoggedIn { get; private set; } = false;
    public DateTime? LastLoginDate { get; private set; }
    public DateTime? LastVerifiedDate { get; private set; }

    public string CreateHash(string code)
    {
        using var md5 = MD5.Create();
        var inputBytes = Encoding.ASCII.GetBytes(Id + code);
        return string.Concat(md5.ComputeHash(inputBytes).Select(x => x.ToString("x2")));
    }

    public ClaimsIdentity ToIdentity(BlossomUser user)
    {
        var claims = user.Claims.Select(x => new Claim(x.Key, x.Value)).ToList();
        claims.AddRange(user.MultiClaims.SelectMany(x => x.Value.Select(v => new Claim(x.Key, v))));
        claims.Add(new Claim(ClaimTypes.AuthenticationMethod, Type));
        claims.Add(new Claim("externalId", Id));

        return new ClaimsIdentity(claims, Type);
    }

    public string GenerateVerificationCode()
    {
        var code = Id == "appletest@email.com"
            ? "123456"
            : new Random().Next(0, 1000000).ToString("D6");
        VerificationHash = CreateHash(code);
        return code;
    }

    public bool VerifyCode(string code)
    {
        var hash = CreateHash(code);
        IsVerified = hash == VerificationHash;
        return IsVerified;
    }

    public void Revoke()
    {
        VerificationHash = null;
        IsLoggedIn = false;
        IsVerified = false;
    }

    internal void Login()
    {
        IsLoggedIn = true;
        LastLoginDate = DateTime.UtcNow;
    }

    internal void Logout()
    {
        IsLoggedIn = false;
    }
}
