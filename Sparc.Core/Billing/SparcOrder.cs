namespace Sparc.Core.Billing;

public class SparcOrder
{
    public string ProductId { get; set; } = "";
    public string? Currency { get; set; }
    public string? Email { get; set; }
    public string? PaymentIntentId { get; set; }
}


