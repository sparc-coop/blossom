using Blazored.LocalStorage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace Sparc.Authentication;

public class PasswordlessRequirement : DenyAnonymousAuthorizationRequirement
{
}

public class PasswordlessAuthorizationHandler : AuthorizationHandler<PasswordlessRequirement>
{
    public PasswordlessAuthorizationHandler(ILocalStorageService localStorage) : base()
    {
        LocalStorage = localStorage;
    }

    public ILocalStorageService LocalStorage { get; }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PasswordlessRequirement requirement)
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
        var token = await LocalStorage.GetItemAsync<string>(PasswordlessAccessTokenProvider.TokenName);
        return token != null;
    }
}
