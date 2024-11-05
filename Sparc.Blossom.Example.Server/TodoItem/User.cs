using Sparc.Blossom.Authentication;

namespace Sparc.Blossom.Example.Single.TodoItem;

public class User(string email, string externalId) : BlossomUser(email, "Blossom", externalId)
{
    public string Email { get; set; } = email;
    public string ExternalID { get; set; } = externalId;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}
