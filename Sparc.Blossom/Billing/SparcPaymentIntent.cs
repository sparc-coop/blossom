namespace Sparc.Blossom.Billing;

public class SparcPaymentIntent
{
    [Newtonsoft.Json.JsonProperty("clientSecret")]
    public string ClientSecret { get; set; } = default!;
    public string PublishableKey { get; set; } = default!;
    public string PaymentIntentId { get; set; } = default!;
    public decimal Amount { get; set; } = default!;
    public string Currency { get; set; } = default!;
    public string FormattedAmount {  get; set; } = default!;
}


