namespace Sparc.Blossom.Billing;

public class UserBilling
{
    public UserBilling()
    {
        Currency = "USD";
        TicksBalance = TimeSpan.FromMinutes(10).Ticks; // Initial free minutes
    }

    public void SetUpCustomer(string customerId, string currency)
    {
        CustomerId = customerId;
        Currency = currency;
    }

    public string? CustomerId { get; set; }

    public long TicksBalance { get; set; }
    public string Currency { get; set; }
}
