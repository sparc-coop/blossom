using Blazored.LocalStorage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Sparc.Authentication;

public class SparcAccessTokenRequirement : DenyAnonymousAuthorizationRequirement
{
}

public class SparcAccessTokenAuthorizationHandler : AuthorizationHandler<SparcAccessTokenRequirement>
{
    public SparcAccessTokenAuthorizationHandler(ILocalStorageService localStorage) : base()
    {
        LocalStorage = localStorage;
    }

    public ILocalStorageService LocalStorage { get; }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, SparcAccessTokenRequirement requirement)
    {
        if (await HasValidAccessToken())
        {
            context.Succeed(requirement);
        }
        else
        {
            await requirement.HandleAsync(context);
        }
    }

    private async Task<bool> HasValidAccessToken()
    {
        var token = await LocalStorage.GetItemAsync<string>(SparcAccessTokenProvider.TokenName);
        return token != null;
    }
}
