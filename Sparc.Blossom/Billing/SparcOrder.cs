namespace Sparc.Blossom.Billing;

public class SparcOrder() : BlossomEntity<string>(RandomSerialNumber(4, 3))  
{
    public string OrderId {  get { return Id; } set { Id = value;  } } // Partition key
    public string ProductId { get; set; } = "";
    public string TierId { get; set; } = "";
    public string UserId { get; set; } = "";
    public string Domain { get; set; } = "";
    public string? PaymentIntentId { get; set; }
    public long Amount { get; set; }
    public string? Currency { get; set; }
    public string? Email { get; set; }
    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
    public DateTime? FulfilledDate { get; set; }

    public void Fulfill()
    {
        FulfilledDate = DateTime.UtcNow;
    }

    private static string RandomSerialNumber(int numSections, int charsPerSection)
    {
        var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
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


