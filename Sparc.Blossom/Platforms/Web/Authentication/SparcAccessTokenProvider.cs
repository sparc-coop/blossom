using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;
using Microsoft.Extensions.DependencyInjection;
using System.Web;

namespace Sparc.Authentication;

public class SparcAccessTokenProvider : IAccessTokenProvider
{
    public static readonly string TokenName = "_sparc_access_token";

    public SparcAccessTokenProvider(ILocalStorageService localStorage, NavigationManager navigation)
    {
        LocalStorage = localStorage;
        Navigation = navigation;
    }

    public ILocalStorageService LocalStorage { get; }
    public NavigationManager Navigation { get; }

    public async Task SetAccessToken()
    {
        var uri = new Uri(Navigation.Uri);

        var queryString = HttpUtility.ParseQueryString(uri.Query);
        if (queryString["token"] != null)
        {
            await LocalStorage.SetItemAsync(TokenName, queryString["token"]);
            Navigation.NavigateTo(queryString["returnUrl"]!);
        }
    }

    public async ValueTask<AccessTokenResult> RequestAccessToken()
    {
        return await RequestAccessTokenWithOptionalOptions();
    }

    public async ValueTask<AccessTokenResult> RequestAccessToken(AccessTokenRequestOptions options)
    {
        return await RequestAccessTokenWithOptionalOptions(options);
    }

    public async ValueTask<AccessTokenResult> RequestAccessTokenWithOptionalOptions(AccessTokenRequestOptions? options = null)
    {
        // Check local storage for access token
        var token = await LocalStorage.GetItemAsync<string>(TokenName);
        if (token == null)
        {
            return new AccessTokenResult(AccessTokenResultStatus.RequiresRedirect,
                null,
                "/authentication/login",
                new InteractiveRequestOptions
                {
                    Interaction = InteractionType.GetToken,
                    ReturnUrl = options?.ReturnUrl != null ? Navigation.ToAbsoluteUri(options.ReturnUrl).AbsoluteUri : Navigation.Uri,
                    Scopes = options?.Scopes ?? Array.Empty<string>()
                });
        }

        return new AccessTokenResult(AccessTokenResultStatus.Success, new AccessToken { Value = token }, null, null);
    }
}

public sealed class SparcAccessTokenProviderAccessor : IAccessTokenProviderAccessor
{
    private readonly IServiceProvider _provider;
    private IAccessTokenProvider? _tokenProvider;

    public SparcAccessTokenProviderAccessor(IServiceProvider provider) => _provider = provider;

    public IAccessTokenProvider TokenProvider => _tokenProvider ??= _provider.GetRequiredService<IAccessTokenProvider>();
}