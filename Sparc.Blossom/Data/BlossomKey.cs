using System.Text;

namespace Sparc.Core;

public class BlossomKey
{
    public string MaskedKey { get; set; } = "";
    public string Hash { get; set; } = "";

    public override string ToString() => MaskedKey;
    
    public static (BlossomKey, string Key) Create(int length = 32)
    {
        var key = RandomCharacters(length);
        var blossomKey = new BlossomKey
        {
            MaskedKey = Mask(key, 4, 4),
            Hash = SHA256(key)
        };
        return (blossomKey, key);
    }
    
    public static string SHA256(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        var sb = new StringBuilder();
        for (int i = 0; i < hash.Length; i++)
        {
            sb.Append(hash[i].ToString("x2"));
        }
        return sb.ToString();
    }

    public static string RandomCharacters(int length)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    public static string Mask(string input, int prefixLength, int suffixLength)
    {
        if (input.Length <= prefixLength + suffixLength)
            // Mask all by default
            return new string('*', input.Length);

        var maskedPart = new string('*', input.Length - prefixLength - suffixLength);
        return $"{input.Substring(0, prefixLength)}{maskedPart}{input.Substring(input.Length - suffixLength)}";
    }
}
