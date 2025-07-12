using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public class BlossomUser : BlossomEntity<string>, IEquatable<BlossomUser>
{
    public BlossomUser()
    {
        Id = Guid.NewGuid().ToString();
        DateCreated = DateTime.UtcNow;
    }

    public string UserId { get { return Id; } set { Id = value; } }
    public DateTime DateCreated { get; private set; }
    public DateTime DateModified { get; private set; }
    public DateTime? LastLogin { get; private set; }
    public string? LastPageVisited { get; set; }

    internal Dictionary<string, string> Claims { get; set; } = [];
    internal Dictionary<string, IEnumerable<string>> MultiClaims { get; set; } = [];

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

    public virtual void RegisterClaims()
    {
        // Do nothing in base class. This should be overridden in derived classes to
        // create the claims from the persisted user.
    }

    void RegisterBaseClaims()
    {
        AddClaim(ClaimTypes.NameIdentifier, Id);
        RegisterClaims();
    }

    public virtual ClaimsPrincipal ToPrincipal()
    {
        RegisterBaseClaims();

        var defaultIdentity = new BlossomIdentity(Id, "Anonymous");

        return new ClaimsPrincipal(defaultIdentity.ToIdentity(this));
    }

    public void Login()
    {
        LastLogin = DateTime.UtcNow;
    }

    public ClaimsPrincipal Logout()
    {
        return ToPrincipal();
    }

    public static BlossomUser FromPrincipal(ClaimsPrincipal principal) => 
        FromPrincipal<BlossomUser>(principal);

    public static T FromPrincipal<T>(ClaimsPrincipal principal)
        where T : BlossomUser, new()
    {
        var user = new T();
        var id = principal.Id();
        if (!string.IsNullOrWhiteSpace(id))
        {
            user.Id = id;
        }

        foreach (var claim in principal.Claims)
            user.AddClaim(claim.Type, claim.Value);

        return user;
    }

    public bool Equals(BlossomUser other)
    {
        if (Id != other.Id) return false;
       
        var orderedPriorClaims = Claims.OrderBy(x => x.Key).ThenBy(x => x.Value);
        var orderedClaims = other.Claims.OrderBy(x => x.Key).ThenBy(x => x.Value);

        var hasDifferentClaims = orderedClaims.Count() != orderedPriorClaims.Count() ||
            orderedClaims.Zip(orderedPriorClaims, (a, b) => a.Key != b.Key || a.Value != b.Value).Any(x => x);

        return !hasDifferentClaims;
    }
}
