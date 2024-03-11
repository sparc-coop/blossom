namespace Sparc.Blossom.Authentication;

public abstract class BlossomAuthenticator
{
    public abstract Task<BlossomUser?> RegisterAsync(string userName);
    public abstract Task<BlossomUser?> LoginAsync(string token);
    public abstract Task<BlossomUser?> GetAsync();
}

public abstract class BlossomAuthenticator<TUser> where TUser : BlossomUser, new()
{
    public abstract Task<TUser?> RegisterAsync(string userName);
    public abstract Task<TUser?> LoginAsync(string token);
    public abstract Task<TUser?> GetAsync();
}
