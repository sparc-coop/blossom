using Microsoft.Maui.Essentials;
using System.Threading.Tasks;

namespace Sparc.Platforms.Maui;

public class PushTokenProvider
{
    internal async Task UpdateTokenAsync(string token)
    {
        await SecureStorage.SetAsync("sparc-device-id", token);
    }

    internal async Task<string> GetTokenAsync() => await SecureStorage.GetAsync("sparc-device-id");
}
