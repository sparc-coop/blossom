namespace Sparc.Blossom.Authentication;

public record BlossomLogin(BlossomUser User, string? AccessToken = null)
{
    public BlossomUser ToUser()
    {
        if (AccessToken != null)
            User.Claims["sparcaura-access"] = AccessToken;
        
        return User;
    }
}