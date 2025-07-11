namespace Sparc.Blossom.Billing;

public record CreateOrderPaymentRequest(
    long Amount,
    string Currency,
    string? CustomerId,
    string? ReceiptEmail,
    Dictionary<string, string>? Metadata,
    string? SetupFutureUsage
);

public class CreateOrderRequest()
{
    public long Amount { get; set; }
    public string Currency { get; set; }
    public string? CustomerId { get; set; }
    public string? ReceiptEmail { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
    public string? SetupFutureUsage { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Address { get; set; }
    public string Address2 { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string Country { get; set; }
    public string Zip { get; set; }
    public string Phone { get; set; }
}

public class CreateOrderPaymentResponse
{
    [Newtonsoft.Json.JsonProperty("clientSecret")]
    public string ClientSecret { get; set; } = default!;
}

public record ConfirmOrderPaymentRequest(string PaymentIntentId, string PaymentMethodId);


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
