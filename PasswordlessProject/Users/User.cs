using Sparc.Blossom.Authentication;

namespace PasswordlessProject;

public class User : BlossomUser
{
    public User() : base()
    {
        UserId = Id;
    }

    public string UserId { get; private set; }
    public string? Email { get; private set; }
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? CurrentKitId { get; private set; }

    protected override void RegisterClaims()
    {
        AddClaim("CurrentKitId", CurrentKitId);
    }
}
