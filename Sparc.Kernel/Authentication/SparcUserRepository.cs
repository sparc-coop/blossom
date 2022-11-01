using Microsoft.AspNetCore.Identity;
using Sparc.Core;

namespace Sparc.Authentication;

public class SparcUserRepository<T> : IUserSecurityStampStore<T> where T : SparcUser
{
    public SparcUserRepository(IRepository<T> users)
    {
        Users = users;
    }

    public IRepository<T> Users { get; }

    public async Task<IdentityResult> CreateAsync(T user, CancellationToken cancellationToken)
    {
        await Users.AddAsync(user);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(T user, CancellationToken cancellationToken)
    {
        await Users.DeleteAsync(user);
        return IdentityResult.Success;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public async Task<T?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        return await Users.FindAsync(userId);
    }

    public Task<T?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        var user = Users.Query.FirstOrDefault(x => x.UserName == normalizedUserName);
        return Task.FromResult(user);
    }

    public Task<string?> GetNormalizedUserNameAsync(T user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.UserName);
    }

    public Task<string?> GetSecurityStampAsync(T user, CancellationToken cancellationToken)
    {
        return Task.FromResult((string?)user.SecurityStamp);
    }

    public Task<string> GetUserIdAsync(T user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.Id);
    }

    public Task<string?> GetUserNameAsync(T user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.UserName);
    }

    public async Task SetNormalizedUserNameAsync(T user, string? normalizedName, CancellationToken cancellationToken)
    {
        user.UserName = normalizedName;
        await UpdateAsync(user, cancellationToken);
    }

    public async Task SetSecurityStampAsync(T user, string stamp, CancellationToken cancellationToken)
    {
        user.SecurityStamp = stamp;
        await UpdateAsync(user, cancellationToken);
    }

    public async Task SetUserNameAsync(T user, string? userName, CancellationToken cancellationToken)
    {
        user.UserName = userName;
        await UpdateAsync(user, cancellationToken);
    }

    public async Task<IdentityResult> UpdateAsync(T user, CancellationToken cancellationToken)
    {
        await Users.UpdateAsync(user);
        return IdentityResult.Success;
    }
}
