using Sparc.Engine;
using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public class BlossomUser : BlossomEntity<string>, IEquatable<BlossomUser>
{
    public BlossomUser()
    {
        Id = Guid.NewGuid().ToString();
        Avatar = new(Id, "");
        DateCreated = DateTime.UtcNow;
    }

    public string UserId { get { return Id; } set { Id = value; } }
    public DateTime DateCreated { get; private set; }
    public DateTime DateModified { get; private set; }
    public DateTime? LastLogin { get; private set; }
    public string? LastPageVisited { get; set; }
    public string? Token { get; set; }
    public BlossomAvatar Avatar { get; set; } = new();

    public List<BlossomIdentity> Identities { get; set; } = [];
    public List<SparcProduct> Products { get; set; } = [];
    internal Dictionary<string, string> Claims { get; set; } = [];
    internal Dictionary<string, IEnumerable<string>> MultiClaims { get; set; } = [];
    public string? Identity(string authenticationType) => 
        Identities.FirstOrDefault(x => x.Type == authenticationType)?.Id;

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
        AddClaim(ClaimTypes.Name, Avatar.Username);
        if (Avatar.Language != null)
            AddClaim("language", Avatar.Language.Id);
        if (Avatar.Locale != null)
            AddClaim("locale", Avatar.Locale.Id);
        if (Avatar.Currency != null)
            AddClaim("currency", Avatar.Currency.Id);
        
        RegisterClaims();
    }

    public virtual ClaimsPrincipal ToPrincipal()
    {
        RegisterBaseClaims();

        var defaultIdentity = Identities.FirstOrDefault()
            ?? new BlossomIdentity(Id, "Anonymous");

        return new ClaimsPrincipal(defaultIdentity.ToIdentity(this));
    }

    public ClaimsPrincipal ToPrincipal(string authenticationType, string externalId)
    {
        RegisterBaseClaims();
        var identity = GetOrCreateIdentity(authenticationType, externalId);
        return new ClaimsPrincipal(identity.ToIdentity(this));
    }

    public void ChangeUsername(string username)
    {
        Avatar.Username = username;
    }

    public SparcProduct AddProduct(string productId)
    {
        var existing = Products.FirstOrDefault(x => x.ProductId == productId);
        if (existing != null)
            return existing;

        var product = new SparcProduct(productId);
        Products.Add(product);

        return product;
    }

    public bool HasProduct(string productName)
    {
        return Products.Any(x => x.ProductId.Equals(productName, StringComparison.OrdinalIgnoreCase));
    }

    public void Login()
    {
        LastLogin = DateTime.UtcNow;
    }

    public ClaimsPrincipal Logout()
    {
        Identities.ForEach(x => x.Logout());
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
            user.Avatar.Username = principal.Get(ClaimTypes.Name) ?? id;
        }

        foreach (var claim in principal.Claims)
            user.AddClaim(claim.Type, claim.Value);

        return user;
    }

    public bool Equals(BlossomUser other)
    {
        if (Id != other.Id) return false;
        if (Avatar.Username != other.Avatar.Username) return false;
        if (Avatar.Language != other.Avatar.Language) return false;
        if (Avatar.Locale?.Id != other.Avatar.Locale?.Id) return false;

        var orderedPriorClaims = Claims.OrderBy(x => x.Key).ThenBy(x => x.Value);
        var orderedClaims = other.Claims.OrderBy(x => x.Key).ThenBy(x => x.Value);

        var hasDifferentClaims = orderedClaims.Count() != orderedPriorClaims.Count() ||
            orderedClaims.Zip(orderedPriorClaims, (a, b) => a.Key != b.Key || a.Value != b.Value).Any(x => x);

        return !hasDifferentClaims;
    }

    public void ChangeVoice(Language language, Voice? voice = null)
    {
        ChangeLanguage(language);

        Avatar.Language = language with { DialectId = voice?.Locale, VoiceId = voice?.ShortName };
        Avatar.Gender = voice?.Gender;
    }

    public void ChangeLanguage(Language language)
    {
        if (!Avatar.LanguagesSpoken.Any(x => x.Matches(language)))
            Avatar.LanguagesSpoken.Add(language);

        Avatar.Language = language;
    }

    public void UpdateAvatar(BlossomAvatar avatar)
    {
        Avatar.Id = Id;
        Avatar.Language = avatar.Language;
        Avatar.Locale = avatar.Locale;
        Avatar.Currency = avatar.Currency;
        Avatar.BackgroundColor = avatar.BackgroundColor;
        Avatar.Pronouns = avatar.Pronouns;
        Avatar.Name = avatar.Name;
        Avatar.Description = avatar.Description;
        Avatar.SkinTone = avatar.SkinTone;
        Avatar.Emoji = avatar.Emoji;
        Avatar.HearOthers = avatar.HearOthers;
        Avatar.MuteMe = avatar.MuteMe;
    }

    internal void GoOnline(string connectionId)
    {
        Avatar.IsOnline = true;
    }

    internal void GoOffline()
    {
        Avatar.IsOnline = false;
    }

    public bool HasIdentity(string authenticationType)
    {
        return Identities.Any(x => x.Type == authenticationType);
    }

    public BlossomIdentity GetOrCreateIdentity(string authenticationType, string externalId)
    {
        var identity = Identities.FirstOrDefault(x => x.Type == authenticationType && x.Id == externalId)
            ?? AddIdentity(authenticationType, externalId);

        return identity;
    }

    public BlossomIdentity AddIdentity(string authenticationType, string externalId)
    {
        var identity = new BlossomIdentity(externalId, authenticationType);
        Identities.Add(identity);

        Avatar.VerificationLevel = Identity("Email") != null ? 2
            : Identity("Passkey") != null ? 1
            : 0;

        return identity;
    }

    public SparcProduct? Product(string productId) => Products.FirstOrDefault(x => x.ProductId == productId);
}
