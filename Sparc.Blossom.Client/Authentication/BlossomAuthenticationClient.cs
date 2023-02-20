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
        var user = await _httpClient.GetFromJsonAsync<BlossomUser>("userinfo");
        return user?.CreatePrincipal()
            ?? new ClaimsPrincipal(new ClaimsIdentity());
    }
}