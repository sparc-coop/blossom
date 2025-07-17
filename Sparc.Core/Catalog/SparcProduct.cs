namespace Sparc.Engine;

public class SparcProduct(string productId)
{
    public SparcProduct() : this("")
    {
    }

    public string ProductId { get; set; } = productId;
    public string SerialNumber { get; set; } = RandomSerialNumber(2, 5);
    public string? UserId { get; set; }

    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
    public int UsageMeter { get; set; } = 0;

    private static string RandomSerialNumber(int numSections, int charsPerSection)
    {
        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();

        var sections = new List<string>();
        for (int i = 0; i < numSections; i++)
        {
            var section = new string(Enumerable.Repeat(chars, charsPerSection)
                .Select(s => s[random.Next(s.Length)]).ToArray());
            sections.Add(section);
        }

        return string.Join("-", sections);
    }
}

public record GetProductResponse
(
    string Id,
    string Name,
    decimal Price,
    string Currency,
    string FormattedPrice
);