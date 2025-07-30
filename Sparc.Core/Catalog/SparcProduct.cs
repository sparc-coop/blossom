namespace Sparc.Engine;

public class SparcProduct(string productId)
{
    public SparcProduct() : this("")
    {
    }

    public string ProductId { get; set; } = productId;
    public List<string> OrderIds { get; set; } = []; 
    public int MaxUsage { get; set; } = 0;
    public decimal TotalUsage { get; set; } = 0;
    public bool HasExceededUsage => TotalUsage > MaxUsage;
}

public record GetProductResponse
(
    string Id,
    string Name,
    decimal Price,
    string Currency,
    string FormattedPrice
);