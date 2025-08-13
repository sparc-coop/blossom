using OtpNet;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace Sparc.Blossom.Authentication;

public record SparcCodeIndex(string Hash, string UserId, byte[] UserSecret, DateTime Expires);
public class SparcCodes
{
    static readonly ConcurrentDictionary<string, SparcCodeIndex> TotpIndex = [];
    static readonly VerificationWindow Window = VerificationWindow.RfcSpecifiedNetworkDelay;
    static readonly int TotpSize = 8;

    public static SparcCode? Generate(BlossomUser user)
    {
        var secretKey = UserSecret(user);
        if (secretKey == null)
            return null;

        SparcCode? code;
        do
        {
            code = Generate(secretKey);
            var index = GenerateIndex(code, user.Id, secretKey);
            if (!TotpIndex.TryAdd(index.Hash, index))
                code = null;
        } while (code == null);

        return code;
    }

    public static string? Verify(string code)
    {
        CleanUpExpiredEntries();

        var cleanCode = code.Replace("totp:", "").Replace("-", "");
        var hash = Hash(cleanCode);
        if (!TotpIndex.TryGetValue(hash, out var index))
            return null;

        // Validate against the user secret
        var totp = new Totp(index.UserSecret, totpSize: TotpSize);
        if (!totp.VerifyTotp(cleanCode, out long timeStepMatched, Window))
            return null;

        // TODO: Validate against timeStepMatched

        // If the code is valid, remove it from the index
        TotpIndex.TryRemove(hash, out _);
        return index.UserId;
    }

    private static SparcCode Generate(byte[] secretKey)
    {
        var totp = new Totp(secretKey, totpSize: TotpSize);
        return new SparcCode(totp.ComputeTotp(), totp.RemainingSeconds());
    }

    private static SparcCodeIndex GenerateIndex(SparcCode code, string userId, byte[] secretKey)
    {
        var hash = Hash(code.Code);
        var expires = DateTime.UtcNow.AddSeconds(code.RemainingSeconds * 10);
        SparcCodeIndex index = new(hash, userId, secretKey, expires);
        return index;
    }

    private static byte[]? UserSecret(BlossomUser user)
    {
        // TODO: Implement a way to retrieve or generate a user-specific secret key.
        return GenerateRandomKey();
    }

    private static void CleanUpExpiredEntries()
    {
        var now = DateTime.UtcNow;
        foreach (var key in TotpIndex.Keys)
        {
            if (TotpIndex.TryGetValue(key, out var entry) && entry.Expires < now)
                TotpIndex.TryRemove(key, out _);
        }
    }

    private static string Hash(string code) => Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(code)));
    public static byte[] GenerateRandomKey(int length = 32)
    {
        var key = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(key);
        
        return key;
    }
}
