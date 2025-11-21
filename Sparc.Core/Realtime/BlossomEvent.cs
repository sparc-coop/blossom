using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Sparc.Blossom.Realtime;

public class BlossomEvent(string roomId, string sender) : BlossomEntity<string>(), MediatR.INotification
{
    public string Type { get; set; } = "";
    public string EventId { get { return Id; } set { Id = value; } }
    public long Depth { get; set; } = 1;
    public long OriginServerTs { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    public List<string> PrevEvents { get; set; } = [];
    public string RoomId { get; set; } = roomId;
    public string Sender { get; set; } = sender;
    public string? StateKey { get; set; }

    // For event signing and verification 
    public MatrixEventHash Hashes { get; set; } = null!;
    public Dictionary<string, Dictionary<string, string>> Signatures { get; set; } = [];
    public MatrixUnsignedData Unsigned => new(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - OriginServerTs);

    public static BlossomEvent<T> Create<T>(string roomId, string sender, T content, List<BlossomEvent>? previousEvents = null)
    {
        return new BlossomEvent<T>(roomId, sender, content, previousEvents);
    }

    public virtual void ApplyTo(BlossomSpace room)
    { 
    }
    
    //// Special magic to be able to save & query polymorphically to/from Cosmos
    //public static string Types<T>() =>  
    //    MatrixEventTypes.TryGetValue(typeof(BlossomEvent<>).MakeGenericType(typeof(T)), out var type) 
    //    ? type 
    //    : throw new NotImplementedException($"Matrix event type for {typeof(T).Name} is not implemented.");

    //private static Dictionary<Type, string> MatrixEventTypes =>
    //    typeof(BlossomEvent)
    //        .GetCustomAttributes(typeof(JsonDerivedTypeAttribute), false)
    //        .OfType<JsonDerivedTypeAttribute>()
    //        .ToDictionary(attr => attr.DerivedType, attr => attr.TypeDiscriminator!.ToString()!);

    public static string OpaqueId(int length = 64)
    {
        // Generate a random string of characters and digits
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        var result = new char[length];
        for (int i = 0; i < length; i++)
            result[i] = chars[random.Next(chars.Length)];

        return new string(result);
    }
}

public class BlossomEvent<T> : BlossomEvent
{
    public T Content { get; set; }

    public BlossomEvent() : this(string.Empty, string.Empty, default!)
    {
    }

    public BlossomEvent(string roomId, string sender, T content, List<BlossomEvent>? previousEvents = null) 
        : base(roomId, sender)
    {
        Type = typeof(T).Name;
        Content = content;

        if (previousEvents != null && previousEvents.Count > 0)
        {
            PrevEvents = previousEvents
                .OrderByDescending(x => x.OriginServerTs)
                .Take(20)
                .Select(e => e.EventId)
                .ToList();
            Depth = previousEvents.Max(e => e.Depth) + 1;
        }

        Id = "$" + UnpaddedBase64(ReferenceHash());
    }

    public byte[] ReferenceHash()
    {
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false
        });
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
    }

    public string UnpaddedBase64(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
                    .Replace("=", "")
                    .Replace("+", "-")
                    .Replace("/", "_");
    }

    public override void ApplyTo(BlossomSpace space)
    {
        if (Content is IMatrixRoomEvent ev)
            ev.ApplyTo(space);
    }
}

public interface IMatrixRoomEvent
{
    void ApplyTo(BlossomSpace space);
}

public record MatrixEventHash(string Sha256);
public record MatrixUnsignedData(long Age);