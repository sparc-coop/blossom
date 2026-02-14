namespace Sparc.Blossom.Billing;

public class SparcProduct(string productId)
{
    public SparcProduct() : this("")
    {
    }

    public string ProductId { get; set; } = productId;
    public string TierId { get; set; } = "Free";
    public List<string> OrderIds { get; set; } = []; 
    public int MaxUsage { get; set; } = 0;
}

public record SparcProductActivationOptions(int MaxUsage);

public record ProductTier(string Name, decimal Price, string FormattedPrice);
public record GetProductResponse
(
    string Id,
    string Name,
    string Currency,
    List<ProductTier> Tiers
);