using Blazored.LocalStorage;
using Microsoft.AspNetCore.Authorization;

namespace Sparc.Blossom.Authentication;

public class BlossomAccessTokenAuthorizationHandler : AuthorizationHandler<BlossomAccessTokenRequirement>
{
    public BlossomAccessTokenAuthorizationHandler(ILocalStorageService localStorage) : base()
    {
        LocalStorage = localStorage;
    }

    public ILocalStorageService LocalStorage { get; }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, BlossomAccessTokenRequirement requirement)
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
        var token = await LocalStorage.GetItemAsync<string>(BlossomAuthenticationStateProvider.TokenName);
        return token != null;
    }
}
