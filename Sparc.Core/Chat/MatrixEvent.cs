using Sparc.Blossom;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sparc.Core.Chat;

[JsonDerivedType(typeof(MatrixEvent<MatrixMessage>), "m.room.message")]
[JsonDerivedType(typeof(MatrixEvent<CreateRoom>), "m.room.create")]
public class MatrixEvent(string roomId, string sender) : BlossomEntity<string>(), MediatR.INotification
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

    public static MatrixEvent<T> Create<T>(string roomId, string sender, T content, List<MatrixEvent>? previousEvents = null)
    {
        return new MatrixEvent<T>(roomId, sender, content, previousEvents);
    }
    
    // Special magic to be able to save & query polymorphically to/from Cosmos
    public static string Types<T>() =>  
        MatrixEventTypes.TryGetValue(typeof(MatrixEvent<>).MakeGenericType(typeof(T)), out var type) 
        ? type 
        : throw new NotImplementedException($"Matrix event type for {typeof(T).Name} is not implemented.");

    private static Dictionary<Type, string> MatrixEventTypes =>
        typeof(MatrixEvent)
            .GetCustomAttributes(typeof(JsonDerivedTypeAttribute), false)
            .OfType<JsonDerivedTypeAttribute>()
            .ToDictionary(attr => attr.DerivedType, attr => attr.TypeDiscriminator!.ToString());
}

public class MatrixEvent<T> : MatrixEvent
{
    public T Content { get; set; }

    public MatrixEvent() : this(string.Empty, string.Empty, default!)
    {
    }

    public MatrixEvent(string roomId, string sender, T content, List<MatrixEvent>? previousEvents = null) 
        : base(roomId, sender)
    {
        Type = Types<T>();
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
}

public record MatrixEventHash(string Sha256);
public record MatrixUnsignedData(long Age);