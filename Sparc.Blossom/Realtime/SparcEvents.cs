using Microsoft.AspNetCore.Components;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Billing;

namespace Sparc.Blossom.Realtime;

public class SparcEvents
{
    public event Func<SparcCurrency, Task>? CurrencyChanged;
    public async Task OnCurrencyChanged(SparcCurrency currency)
    {
        if (CurrencyChanged != null)
            await CurrencyChanged.Invoke(currency);
    }

    public event Func<BlossomAvatar, Task>? AvatarChanged;
    public async Task OnAvatarChanged(BlossomAvatar avatar)
    {
        if (AvatarChanged != null)
            await AvatarChanged.Invoke(avatar);
    }

    public event Func<Task> SetupProfile;
    public async Task OnSetupProfile()
    {
        if (SetupProfile != null)
            await SetupProfile.Invoke();
    }
}
