using System.Text;

namespace Sparc.Core;

public static class BlossomHash
{
    public static string MD5(string input)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = System.Security.Cryptography.MD5.Create().ComputeHash(bytes);
        var sb = new StringBuilder();
        for (int i = 0; i < hash.Length; i++)
        {
            sb.Append(hash[i].ToString("x2"));
        }
        return sb.ToString();
    }
}
