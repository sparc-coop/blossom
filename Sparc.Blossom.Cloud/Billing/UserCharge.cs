using Stripe;

namespace Kori;

public class UserCharge : BlossomEntity<string>
{
    internal string UserId { get; set; }
    internal string? RoomId { get; set; }
    internal string? MessageId { get; set; }
    internal string Description { get; set; }
    internal DateTime Timestamp { get; set; }
    public decimal Amount { get; set; }
    internal long Ticks { get; set; }
    internal string Currency { get; set; }
    internal string? PaymentIntent { get; set; }

    internal UserCharge()
    {
        UserId = "";
        Currency = "";
        Description = "";
    }

    internal UserCharge(string userId, PaymentIntent paymentIntent)
    {
        Id = paymentIntent.Id;
        UserId = userId;
        Description = "Funds Added";
        Timestamp = DateTime.UtcNow;
        Currency = paymentIntent.Currency.ToUpper();
        //Amount = paymentIntent.LocalAmount();
        PaymentIntent = paymentIntent.ToJson();

        Ticks = paymentIntent.Metadata.TryGetValue("Ticks", out var ticksStr) && long.TryParse(ticksStr, out var ticksVal)
            ? ticksVal
            : 0;
    }

    //internal UserCharge(Room room, CostIncurred cost, User user)
    //{
    //    Id = Guid.NewGuid().ToString();
    //    UserId = user.Id;
    //    RoomId = room.Id;
    //    MessageId = cost.Message?.Id;
    //    Description = cost.Description;
    //    Amount = 0;
    //    Ticks = cost.Ticks;
    //    Timestamp = DateTime.UtcNow;
    //    Currency = "Ticks";
    //}
}
