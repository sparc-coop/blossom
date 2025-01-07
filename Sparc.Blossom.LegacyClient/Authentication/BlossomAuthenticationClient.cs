using System.Net.Http.Json;
using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public class BlossomAuthenticationClient
{
    private readonly HttpClient _httpClient;

    public BlossomAuthenticationClient(HttpClient client)
    {
        _httpClient = client;
    }

    internal async Task<ClaimsPrincipal> GetUserAsync()
    {
        ClaimsPrincipal principal = new ClaimsPrincipal(new ClaimsIdentity());
        try
        {
            var user = await _httpClient.GetFromJsonAsync<BlossomUser>("userinfo");
            if (user != null)
                principal = user.CreatePrincipal();
        }
        catch (HttpRequestException e)
        {
            if (e.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                return principal;
        }

        return principal;
    }
}