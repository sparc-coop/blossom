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
}
