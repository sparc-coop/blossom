namespace Sparc.Blossom.Billing;

public record CreateOrderPaymentRequest(
    long Amount,
    string Currency,
    string? CustomerId,
    string? ReceiptEmail,
    Dictionary<string, string>? Metadata,
    string? SetupFutureUsage
);

public class CreateOrderPaymentResponse
{
    [Newtonsoft.Json.JsonProperty("clientSecret")]
    public string ClientSecret { get; set; } = default!;
}

public record GetProductResponse
(
    string Id,
    string Name,
    long Price,
    string Currency,
    bool IsActive,
    List<Dictionary<string, long>> Prices
);

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
