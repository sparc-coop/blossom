using Microsoft.AspNetCore.Identity;
using Sparc.Blossom.Data;
using Sparc.Blossom.Realtime;
using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public class BlossomUser : Entity<string>
{
    public string? LoginProviderKey { get; set; }
    public IdentityUser Identity { get; } = new();

    public Dictionary<string, string> Claims { get; set; } = new();
    public Dictionary<string, IEnumerable<string>> MultiClaims { get; set; } = new();

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

        if (!MultiClaims.TryAdd(type, values))
            MultiClaims[type] = values;
    }

    protected virtual void RegisterClaims()
    {
        // Do nothing in base class. This should be overridden in derived classes to
        // create the claims from the persisted user.
    }

    public virtual ClaimsPrincipal CreatePrincipal()
    {
        AddClaim(ClaimTypes.NameIdentifier, Id);
        AddClaim(ClaimTypes.Name, Identity.UserName);
        RegisterClaims();

        var claims = Claims.Select(x => new Claim(x.Key, x.Value)).ToList();
        claims.AddRange(MultiClaims.SelectMany(x => x.Value.Select(v => new Claim(x.Key, v))));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Blossom"));
    }

    internal List<INotification>? _events;

    public void Broadcast(INotification notification)
    {
        _events ??= new List<INotification>();
        _events!.Add(notification);
    }

    public List<INotification> Publish()
    {
        if (_events == null || !_events.Any())
            return new();

        var domainEvents = _events.ToList();
        _events.Clear();

        return domainEvents;
    }
}
