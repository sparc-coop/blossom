using Sparc.Blossom.Authentication;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Sparc.Blossom.Realtime;

public record BlossomEvent(string SpaceId)
{
    public string Id { get; set; } = "$" + OpaqueId();
    public string Type { get; set; } = "BlossomEvent";
    public long OriginServerTs { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    public string SubscriptionId => $"{Type}-{SpaceId}";
    public string? UserId { get; set; }

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

    public void SetUser(ClaimsPrincipal? user)
    {
        UserId = user?.Id();
    }
}

public record BlossomEvent<T>(string SpaceId) : BlossomEvent(SpaceId)
{
    public T Data { get; set; } = default!;

    public BlossomEvent(string spaceId, T data)
        : this(spaceId)
    {
        Type = typeof(T).Name;
        Data = data;

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