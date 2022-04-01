using Microsoft.Maui.Essentials;
using System.Threading.Tasks;

namespace Sparc.Platforms.Maui;

public class PushTokenManager
{
    internal async Task UpdateTokenAsync(string token)
    {
        await SecureStorage.SetAsync("pushtoken", token);
    }

    internal async Task<string> GetTokenAsync() => await SecureStorage.GetAsync("pushtoken");
}
