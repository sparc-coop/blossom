using Sparc.Engine;
using System.Security.Claims;
using System.Security.Principal;

namespace Sparc.Blossom.Authentication;

public class BlossomUser : BlossomEntity<string>, IEquatable<BlossomUser>
{
    public BlossomUser()
    {
        Id = Guid.NewGuid().ToString();
        Avatar = new(Id, "");
        DateCreated = DateTime.UtcNow;
    }

    public string Username { get; set; } = "AnonymousUser";
    public string UserId { get { return Id; } set { Id = value; } }
    public DateTime DateCreated { get; private set; }
    public DateTime DateModified { get; private set; }
    public BlossomAvatar Avatar { get; set; } = new();

    public List<BlossomIdentity> Identities { get; set; } = [];
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

    public virtual ClaimsPrincipal ToPrincipal()
    {
        AddClaim(ClaimTypes.NameIdentifier, Id);
        AddClaim(ClaimTypes.Name, Username);
        if (Avatar.Language != null)
            AddClaim("language", Avatar.Language.Id);
        RegisterClaims();

        var claims = Claims.Select(x => new Claim(x.Key, x.Value)).ToList();
        claims.AddRange(MultiClaims.SelectMany(x => x.Value.Select(v => new Claim(x.Key, v))));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Blossom"));
    }

    public ClaimsPrincipal ToPrincipal(string authenticationType, string externalId)
    {
        var identity = GetOrCreateIdentity(authenticationType, externalId);
        AddClaim(ClaimTypes.AuthenticationMethod, authenticationType);
        AddClaim("externalId", externalId);

        return ToPrincipal();
    }

    public void ChangeUsername(string username)
    {
        Username = username;
    }

    public ClaimsPrincipal Logout()
    {
        Identities.ForEach(x => x.Logout());
        return ToPrincipal();
    }

    public static BlossomUser FromPrincipal(ClaimsPrincipal principal)
    {
        var user = new BlossomUser();
        var id = principal.Id();
        if (!string.IsNullOrWhiteSpace(id))
        {
            user.Id = id;
            user.Username = principal.Get(ClaimTypes.Name) ?? id;
        }

        foreach (var claim in principal.Claims)
            user.AddClaim(claim.Type, claim.Value);

        return user;
    }

    public bool Equals(BlossomUser other)
    {
        if (Id != other.Id) return false;
        if (Username != other.Username) return false;
        if (Avatar.Language != other.Avatar.Language) return false;

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

    public static BlossomUser System => new() { Username = "system" };

    public void UpdateAvatar(BlossomAvatar avatar)
    {
        Avatar.Id = Id;
        Avatar.Language = avatar.Language;
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
        return identity;
    }
}
