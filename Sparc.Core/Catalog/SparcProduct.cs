namespace Sparc.Engine;

public class SparcProduct(string productId, string serialNumber, DateTime purchaseDate, string? userId)
{
    public SparcProduct() : this("", "", DateTime.UtcNow, "")
    {
    }

    public string ProductId { get; set; } = productId;
    public string SerialNumber { get; set; } = serialNumber;
    public DateTime PurchaseDate { get; set; } = purchaseDate;
    public string? UserId { get; set;  } = userId;
    public int UsageMeter { get; set; } = 0;
}
