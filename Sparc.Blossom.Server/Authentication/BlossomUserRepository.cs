using Microsoft.AspNetCore.Identity;
using Sparc.Blossom.Data;

namespace Sparc.Blossom.Authentication;

public class BlossomUserRepository<T>(IRepository<T> Users) : IUserSecurityStampStore<T>, IUserEmailStore<T> where T : BlossomUser, new()
{
    public async Task<IdentityResult> CreateAsync(T user, CancellationToken cancellationToken)
    {
        await Users.UpdateAsync(user);
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

    public async Task<T?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        return await FindByNameAsync(normalizedEmail, cancellationToken);
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

    public async Task<string?> GetEmailAsync(T user, CancellationToken cancellationToken)
    {
        return await GetNormalizedUserNameAsync(user, cancellationToken);
    }

    public Task<bool> GetEmailConfirmedAsync(T user, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }

    public async Task<string?> GetNormalizedEmailAsync(T user, CancellationToken cancellationToken)
    {
        return await GetNormalizedUserNameAsync(user, cancellationToken);
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

    public async Task SetEmailAsync(T user, string? email, CancellationToken cancellationToken)
    {
        await SetUserNameAsync(user, email, cancellationToken);
    }

    public Task SetEmailConfirmedAsync(T user, bool confirmed, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task SetNormalizedEmailAsync(T user, string? normalizedEmail, CancellationToken cancellationToken)
    {
        await SetNormalizedUserNameAsync(user, normalizedEmail, cancellationToken);
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
