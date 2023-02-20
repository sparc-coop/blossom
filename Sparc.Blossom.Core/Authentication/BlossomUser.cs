using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public class BlossomUser
{
    public string Id { get; set; } = string.Empty;

    public string? SecurityStamp { get; set; }

    public string? UserName { get; set; }

    public string? LoginProviderKey { get; set; }

    protected Dictionary<string, string> Claims { get; set; } = new();
    protected Dictionary<string, IEnumerable<string>> MultiClaims { get; set; } = new();

    protected void AddClaim(string type, string? value)
    {
        if (value == null)
            return;
        
        if (Claims.ContainsKey(type))
            Claims[type] = value;
        else
            Claims.Add(type, value);
    }

    protected void AddClaim(string type, IEnumerable<string> values)
    {
        if (values == null || !values.Any())
            return;

        if (MultiClaims.ContainsKey(type))
            MultiClaims[type] = values;
        else
            MultiClaims.Add(type, values);
    }

    protected virtual void RegisterClaims()
    {
        // Do nothing in base class. This should be overridden in derived classes to
        // create the claims from the persisted user.
    }

    public virtual ClaimsPrincipal CreatePrincipal()
    {
        AddClaim(ClaimTypes.NameIdentifier, Id);
        AddClaim(ClaimTypes.Name, UserName);
        RegisterClaims();

        var claims = Claims.Select(x => new Claim(x.Key, x.Value)).ToList();
        claims.AddRange(MultiClaims.SelectMany(x => x.Value.Select(v => new Claim(x.Key, v))));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Blossom"));
    }
}
