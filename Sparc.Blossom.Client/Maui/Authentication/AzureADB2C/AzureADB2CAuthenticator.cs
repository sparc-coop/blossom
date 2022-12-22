using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Identity.Client;
using Sparc.Blossom;
using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public class AzureADB2CAuthenticator : AuthenticationStateProvider, IAuthenticator
{
    private readonly IPublicClientApplication _pca;
    public AzureADB2CSettings Settings { get; }
    public static AuthenticationResult AuthResult { get; set; }
    public ClaimsPrincipal User { get; set; }

    public AzureADB2CAuthenticator(AzureADB2CSettings settings)
    {

        // default redirectURI; each platform specific project will have to override it with its own
        var builder = PublicClientApplicationBuilder.Create(settings.ClientID)
            .WithB2CAuthority(settings.AuthoritySignInSignUp)
            .WithIosKeychainSecurityGroup(settings.IOSKeyChainGroup)
            .WithRedirectUri($"msal{settings.ClientID}://auth");

#if ANDROID
        builder = builder.WithParentActivityOrWindow(settings.ParentWindowLocator);
#endif

        _pca = builder.Build();

        Settings = settings;
    }

    public async Task<ClaimsPrincipal> LoginAsync(string returnUrl)
    {
        try
        {
            AuthResult = await AcquireTokenSilent();
        }
        catch (MsalUiRequiredException)
        {
            AuthResult = await SignInInteractively();
        }

        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        User = (await GetAuthenticationStateAsync()).User;
        return User;
    }

    private async Task<AuthenticationResult> AcquireTokenSilent()
    {
        IEnumerable<IAccount> accounts = await _pca.GetAccountsAsync(Settings.PolicySignUpSignIn);
        AuthenticationResult authResult = await _pca.AcquireTokenSilent(Settings.Scopes, GetAccountByPolicy(accounts, Settings.PolicySignUpSignIn))
           .WithB2CAuthority(Settings.AuthoritySignInSignUp)
           .ExecuteAsync();

        return authResult;
    }

    public async Task ResetPasswordAsync()
    {
        AuthResult = await _pca.AcquireTokenInteractive(Settings.Scopes)
            .WithPrompt(Prompt.NoPrompt)
            .WithB2CAuthority(Settings.AuthorityPasswordReset)
            .ExecuteAsync();
    }

    public async Task EditProfileAsync()
    {
        IEnumerable<IAccount> accounts = await _pca.GetAccountsAsync(Settings.PolicyEditProfile);

        var builder = _pca.AcquireTokenInteractive(Settings.Scopes)
            .WithAccount(GetAccountByPolicy(accounts, Settings.PolicyEditProfile))
            .WithPrompt(Prompt.NoPrompt)
            .WithB2CAuthority(Settings.AuthorityEditProfile);

        AuthResult = await builder.ExecuteAsync();
    }

    private async Task<AuthenticationResult> SignInInteractively()
    {
        var builder = _pca.AcquireTokenInteractive(Settings.Scopes).WithPrompt(Prompt.ForceLogin);

        if (DeviceInfo.Platform != DevicePlatform.WinUI)
        {
            SystemWebViewOptions options = new()
            {
                iOSHidePrivacyPrompt = true
            };

            builder.WithSystemWebViewOptions(options);
        }

        try
        {
            return await builder.ExecuteAsync();
        }
        catch (MsalClientException ex) when (ex.ErrorCode == MsalError.AuthenticationCanceledError)
        {
            return null;
        }
    }

    public async Task LogoutAsync()
    {

        IEnumerable<IAccount> accounts = await _pca.GetAccountsAsync(Settings.PolicySignUpSignIn);
        while (accounts.Any())
        {
            await _pca.RemoveAsync(accounts.FirstOrDefault());
            accounts = await _pca.GetAccountsAsync(Settings.PolicySignUpSignIn);
        }

        AuthResult = null;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private IAccount GetAccountByPolicy(IEnumerable<IAccount> accounts, string policy)
    {
        foreach (var account in accounts)
        {
            string userIdentifier = account.HomeAccountId.ObjectId.Split('.')[0];
            if (userIdentifier.EndsWith(policy.ToLower())) return account;
        }

        return null;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (AuthResult?.IdToken == null)
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal()));

        var identity = new ClaimsIdentity(AuthResult.ClaimsPrincipal.Claims, "none");

        if (AuthResult.AccessToken != null)
            identity.AddClaim(new Claim("access_token", AuthResult.AccessToken));

        var user = new ClaimsPrincipal(identity);

        return Task.FromResult(new AuthenticationState(user));
    }

    public Task<bool> LoginAsync()
    {
        throw new NotImplementedException();
    }
}
