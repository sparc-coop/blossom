using Sparc.Blossom.Authentication;
using Sparc.Core.Billing;

namespace Sparc.Engine;

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
}
