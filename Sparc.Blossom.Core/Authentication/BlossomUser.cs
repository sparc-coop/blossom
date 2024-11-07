using Sparc.Blossom.Data;
using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public class BlossomUser : BlossomEntity<string>, IEquatable<BlossomUser>
{
    public BlossomUser()
    {
        Id = Guid.NewGuid().ToString();
        AuthenticationType = "Blossom";
        Username = Id;
    }
    
    public string Username { get; set; }
    public string AuthenticationType { get; set; }
    public string? ExternalId { get; set; }
    public string? ParentUserId { get; set; }

    internal Dictionary<string, string> Claims { get; set; } = [];
    Dictionary<string, IEnumerable<string>> MultiClaims { get; set; } = [];

    public void AddClaim(string type, string? value)
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

        if (!MultiClaims.ContainsKey(type))
            MultiClaims.Add(type, values);
        else
            MultiClaims[type] = values;
    }

    protected void RemoveClaim(string type)
    {
        if (Claims.ContainsKey(type))
            Claims.Remove(type);
        if (MultiClaims.ContainsKey(type))
            MultiClaims.Remove(type);
    }

    protected virtual void RegisterClaims()
    {
        // Do nothing in base class. This should be overridden in derived classes to
        // create the claims from the persisted user.
    }

    public virtual ClaimsPrincipal Login()
    {
        AddClaim(ClaimTypes.NameIdentifier, Id);
        AddClaim(ClaimTypes.Name, Username);
        RegisterClaims();

        var claims = Claims.Select(x => new Claim(x.Key, x.Value)).ToList();
        claims.AddRange(MultiClaims.SelectMany(x => x.Value.Select(v => new Claim(x.Key, v))));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, AuthenticationType ?? "Blossom"));
    }

    public ClaimsPrincipal Login(string authenticationType, string externalId)
    {
        AuthenticationType = authenticationType;
        ExternalId = externalId;
        AddClaim(ClaimTypes.AuthenticationMethod, authenticationType);
        AddClaim("externalId", externalId);

        return Login();
    }

    public void ChangeUsername(string username)
    {
        Username = username;
    }
   
    public void SetParentUser(BlossomUser parentUser)
    {
        Username = parentUser.Username;
        ParentUserId = parentUser.Id;
        ExternalId = parentUser.ExternalId;
    }

    public ClaimsPrincipal Logout()
    {
        ParentUserId = null;
        ExternalId = null;
        AuthenticationType = "Blossom";
        RemoveClaim(ClaimTypes.AuthenticationMethod);
        RemoveClaim("externalId");
        return Login();
    }

    public static BlossomUser FromPrincipal(ClaimsPrincipal principal) => FromPrincipal<BlossomUser>(principal);
    
    public static BlossomUser FromPrincipal<T>(ClaimsPrincipal principal) where T : BlossomUser, new()
    {
        var user = new T();
        var id = principal.Id();
        if (!string.IsNullOrWhiteSpace(id))
        {
            user.Id = id;
            user.ChangeUsername(principal.Get(ClaimTypes.Name) ?? id);
            user.AuthenticationType = principal.Get(ClaimTypes.AuthenticationMethod) ?? "Blossom";
            user.ExternalId = principal.Get("externalId");
        }

        foreach (var claim in principal.Claims)
            user.AddClaim(claim.Type, claim.Value);

        return user;
    }

    public bool Equals(BlossomUser other)
    {
        if (Id != other.Id) return false;
        if (Username != other.Username) return false;
        if (AuthenticationType != other.AuthenticationType) return false;
        if (ExternalId != other.ExternalId) return false;

        var orderedPriorClaims = Claims.OrderBy(x => x.Key).ThenBy(x => x.Value);
        var orderedClaims = other.Claims.OrderBy(x => x.Key).ThenBy(x => x.Value);

        var hasDifferentClaims = orderedClaims.Count() != orderedPriorClaims.Count() ||
            orderedClaims.Zip(orderedPriorClaims, (a, b) => a.Key != b.Key || a.Value != b.Value).Any(x => x);

        return !hasDifferentClaims;
    }
}
