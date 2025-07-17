using Sparc.Blossom;
using Sparc.Engine;

namespace Sparc.Core.Billing;

public class UserCharge() : BlossomEntity<string>(Guid.NewGuid().ToString())
{
    public UserCharge(string userId, TovikContentTranslated tovik) : this()
    {
        UserId = userId;
        Amount = tovik.WordCount;
        InternalCost = tovik.Cost;
        ProductId = "Tovik";
        Currency = "Word";
        Description = tovik.Description;
        Domain = tovik.Content.Domain;
    }
    
    public string UserId { get; set; } = null!;
    public decimal Amount { get; set; }
    public decimal? InternalCost { get; set; }
    public string? Domain { get; set; }
    public string? ProductId { get; set; }
    public string? Currency { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? Description { get; set; }
    public string? PaymentIntentId { get; set; }
}
