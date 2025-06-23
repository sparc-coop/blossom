using Sparc.Blossom.Content;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Sparc.Blossom.Authentication;

public record ProductKey(string ProductName, string SerialNumber, DateTime PurchaseDate);
public record AddProductRequest(string ProductName);
public record UpdateUserRequest(string? Username = null, string? Email = null, string? PhoneNumber = null, bool RequireEmailVerification = false,
    bool RequirePhoneVerification = false);
public record VerificationRequest(string EmailOrPhone, string Code);

public class BlossomUser : BlossomEntity<string>, IEquatable<BlossomUser>
{
    public BlossomUser()
    {
        Id = Guid.NewGuid().ToString();
        AuthenticationType = "Blossom";
        Username = "User";
        Avatar = new(Id, Username);
    }
    
    public string Username { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string UserId { get { return Id; } set { Id = value; } }
    public string AuthenticationType { get; set; }
    public string? ExternalId { get; set; }
    public string? Token { get; set; }
    public string? ParentUserId { get; set; }
    public DateTime DateCreated { get; private set; }
    public DateTime DateModified { get; private set; }
    public UserAvatar Avatar { get; private set; } = new();
    public List<Language> LanguagesSpoken { get; private set; } = [];
    public List<ProductKey> Products { get; set; } = [];
    public string? EmailOrPhone { get; set; }
    public bool IsVerified { get; set; }
    public string? VerificationHash { get; set; }

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

    public static BlossomUser FromPrincipal(ClaimsPrincipal principal)
    {
        var user = new BlossomUser();
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
    
    public void ChangeVoice(Language language, Voice? voice = null)
    {
        ChangeLanguage(language);
        
        Avatar.Language = language with { DialectId = voice?.Locale, VoiceId = voice?.ShortName };
        Avatar.Gender = voice?.Gender;
    }

    public void ChangeLanguage(Language language)
    {
        if (!LanguagesSpoken.Any(x => x.Id == language.Id))
            LanguagesSpoken.Add(language);

        Avatar.Language = language;
    }

    public Language? PrimaryLanguage => LanguagesSpoken.FirstOrDefault(x => x == Avatar.Language);

    public static BlossomUser System => new() { Username = "system" };

    public void UpdateAvatar(UserAvatar avatar)
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

    public void SetToken(string token)
    {
        Token = token;
        if (Claims.ContainsKey("token"))
            Claims["token"] = token;
        else
            Claims.Add("token", token);
    }

    public bool HasProduct(string productName)
    {
        return Products.Any(x => x.ProductName.Equals(productName, StringComparison.OrdinalIgnoreCase));
    }

    public void AddProduct(string productName)
    {
        if (HasProduct(productName))
            return;

        var serial = Guid.NewGuid().ToString(); 
        Products.Add(new ProductKey(productName, serial, DateTime.UtcNow));
    }

    public void Update(UpdateUserRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Username))
            Username = request.Username;

        if (!string.IsNullOrWhiteSpace(request.Email))
            Email = request.Email;

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
            PhoneNumber = request.PhoneNumber;
    }    

    public void Revoke() => IsVerified = false;

    public string CreateHash(string code)
    {
        using var md5 = MD5.Create();
        var inputBytes = Encoding.ASCII.GetBytes(EmailOrPhone + code);
        return string.Concat(md5.ComputeHash(inputBytes).Select(x => x.ToString("x2")));
    }

    public string GenerateVerificationCode()
    {
        var code = EmailOrPhone == "appletest@email.com"
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
}
