using IdentityModel.OidcClient;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;
using Sparc.Core;
using Microsoft.Maui.Essentials;
using System;

namespace Sparc.Platforms.Maui
{
    public class SelfHostedAuthenticator : AuthenticationStateProvider, ISparcAuthenticator
    {
        public ClaimsPrincipal User { get; set; }
        OidcClient Client { get; set; }
        public string AccessToken { get; set; }
        public DateTimeOffset? AccessTokenExpiration { get; set; }
        public string RefreshToken { get; set; }

        public SelfHostedAuthenticator()
        {
        }

        public SelfHostedAuthenticator WithOptions(OidcClientOptions options)
        {
            Client = new OidcClient(options);
            return this;
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            return Task.FromResult(new AuthenticationState(User ?? new ClaimsPrincipal()));
        }

        public async Task<bool> LoginAsync()
        {
            await GetTokensFromSecureStorageAsync();

            if (await TryRefreshTokenAsync())
                return true;

            return await LoginInteractivelyAsync();
        }

        private async Task<bool> LoginInteractivelyAsync()
        {
            var result = await Client.LoginAsync(new LoginRequest());
            if (result.IsError)
                throw new Exception(result.Error);

            User = result?.User;
            AccessToken = result?.AccessToken;
            AccessTokenExpiration = result?.AccessTokenExpiration.ToUniversalTime();
            RefreshToken = result?.RefreshToken;

            await SetTokensToSecureStorageAsync();

            return true;
        }

        private async Task<bool> TryRefreshTokenAsync()
        {
            if (RefreshToken != null && AccessTokenExpiration.HasValue && AccessTokenExpiration.Value.AddSeconds(-30) > DateTime.UtcNow)
            {
                // Use the refresh token
                var refreshResult = await Client.RefreshTokenAsync(RefreshToken);
                if (refreshResult?.IsError == false)
                {
                    AccessToken = refreshResult.AccessToken;
                    AccessTokenExpiration = refreshResult.AccessTokenExpiration.ToUniversalTime();
                    RefreshToken = refreshResult.RefreshToken;
                    await SetTokensToSecureStorageAsync();
                    return true;
                }
                else
                    throw new Exception(refreshResult.Error);
            }

            return false;
        }

        private async Task SetTokensToSecureStorageAsync()
        {
            if (User?.Identity != null && User.Identity is ClaimsIdentity cid)
            {
                if (AccessToken != null)
                {
                    cid.AddClaim(new Claim("access_token", AccessToken));
                    await SecureStorage.SetAsync("AccessToken", AccessToken);
                }

                if (RefreshToken != null)
                {
                    await SecureStorage.SetAsync("RefreshToken", RefreshToken);
                    cid.AddClaim(new Claim("refresh_token", RefreshToken));
                }
            }

            if (AccessTokenExpiration != null)
                await SecureStorage.SetAsync("AccessTokenExpiration", AccessTokenExpiration.Value.ToUnixTimeSeconds().ToString());
        }

        private async Task GetTokensFromSecureStorageAsync()
        {
            if (AccessToken == null)
                AccessToken = await SecureStorage.GetAsync("AccessToken");

            if (RefreshToken == null)
                RefreshToken = await SecureStorage.GetAsync("RefreshToken");

            if (AccessTokenExpiration == null)
            {
                var timestamp = await SecureStorage.GetAsync("AccessTokenExpiration");
                if (timestamp != null)
                    AccessTokenExpiration = DateTimeOffset.FromUnixTimeSeconds(long.Parse(timestamp));
            }
        }

        public async Task LogoutAsync()
        {
            await Client.LogoutAsync(new LogoutRequest());
            User = new ClaimsPrincipal();
            AccessToken = null;
        }
    }
}
