using Foundation;
using System.Text;

namespace Sparc.Platforms.Maui.Mac.Push;

internal static class NSDataExtensions
{
    internal static string ToHexString(this NSData data)
    {
        var bytes = data.ToArray();

        if (bytes == null)
            return null;

        StringBuilder sb = new(bytes.Length * 2);

        foreach (byte b in bytes)
            sb.AppendFormat("{0:x2}", b);

        return sb.ToString().ToUpperInvariant();
    }
}
