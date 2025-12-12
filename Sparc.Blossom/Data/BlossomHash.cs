using System.Text;

namespace Sparc.Core;

public static class BlossomHash
{
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
}
