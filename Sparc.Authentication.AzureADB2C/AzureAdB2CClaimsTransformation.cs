using Microsoft.AspNetCore.Authentication;
using Sparc.Core;
using System.Security.Claims;

namespace Sparc.Authentication.AzureADB2C;

public class AzureAdB2CClaimsTransformation<TUser> : IClaimsTransformation where TUser : ISparcUser
{
    public AzureAdB2CClaimsTransformation(IRepository<TUser> users)
    {
        Users = users;
    }

    public IRepository<TUser> Users { get; }

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.FindFirstValue("iss")?.Contains("b2c") == true)
        {
            var azureId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = Users.Query.Where(u => u.LoginProviderKey == azureId).FirstOrDefault();
            if (user != null)
            {
                var newPrincipal = user.CreatePrincipal();
                return Task.FromResult(newPrincipal);
            }
        }

        return Task.FromResult(principal);
    }
}

