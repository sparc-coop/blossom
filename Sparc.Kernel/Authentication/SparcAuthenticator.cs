using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Sparc.Core;

namespace Sparc.Authentication;

public abstract class SparcAuthenticator<T> where T : SparcUser, new()
{
    public SparcAuthenticator(IHttpContextAccessor httpContextAccessor, IRepository<T> users, IOptionsSnapshot<JwtBearerOptions> config)
    {
        Context = httpContextAccessor.HttpContext;
        Users = users;
        Config = config;
    }

    public HttpContext? Context { get; }
    public IRepository<T> Users { get; }
    public IOptionsSnapshot<JwtBearerOptions> Config { get; }

    public abstract Task<T?> LoginAsync(string userName, string password);
    public async Task<string> CreateTokenAsync(string userId)
    {
        var user = await Users.FindAsync(userId);
        if (user == null)
            throw new Exception($"User {userId} not found!");
       
        return user.CreateToken(Config.Value);
    }
}
