using Sparc.Blossom;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Sparc.Core.Chat;

public class MatrixEvent(string type, string roomId, string sender) : BlossomEntity<string>(), MediatR.INotification
{
    public string EventId { get { return Id; } set { Id = value; } }
    public long Depth { get; set; } = 1;
    public long OriginServerTs { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    public List<string> PrevEvents { get; set; } = [];
    public string RoomId { get; set; } = roomId;
    public string Sender { get; set; } = sender;
    public string? StateKey { get; set; }
    public string Type { get; set; } = type;

    // For event signing and verification 
    public MatrixEventHash Hashes { get; set; }
    public Dictionary<string, Dictionary<string, string>> Signatures { get; set; } = [];
    public MatrixUnsignedData Unsigned => new(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - OriginServerTs);
}

public class MatrixEvent<T> : MatrixEvent
{
    public T Content { get; set; }

    public MatrixEvent(string type, string roomId, string sender, T content, List<MatrixEvent>? previousEvents = null) 
        : base(type, roomId, sender)
    {
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