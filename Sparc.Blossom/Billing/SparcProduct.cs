namespace Sparc.Blossom.Billing;

public class SparcProduct(string productId) : BlossomEntity<string>
{
    public SparcProduct() : this("")
    {
    }

    public string ProductId { get; set; } = productId;
    public string Title { get; set; } = "";
    public string? Subtitle { get; set; }
    public string? Description { get; set; }
    public string? StripeProductId { get; set; }
    public List<ProductTier> Tiers { get; set; } = [];
}

public class SparcLicense(string productId)
{
    public string ProductId { get; set; } = productId;
    public string TierId { get; set; } = "Free";
    public List<string> OrderIds { get; set; } = [];
    public int MaxUsage { get; set; } = 0;
}

public record ProductTier(string Name, decimal Price, int ItemQuantity, string? FormattedPrice = null, string? Description = null);
