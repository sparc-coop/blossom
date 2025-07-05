namespace Sparc.Blossom.Authentication;

public static class HttpContextExtensions
{
    private static readonly HashSet<string> StaticFileExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".js", ".js.map", ".css", ".css.map", ".png", ".jpg", ".jpeg", ".gif", ".svg",
        ".woff", ".woff2", ".ttf", ".ico", ".webp", ".avif", ".mp4", ".webm", ".ogg",
        ".mp3", ".wav", ".flac", ".aac", ".m4a", ".zip", ".pdf", ".txt", ".html", ".htm",
        ".xml", ".json", ".csv", ".md", ".yaml", ".yml", ".webmanifest", ".wasm"
    };

    public static bool IsStaticFileRequest(this HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/_blazor") ||
            context.Request.Path.StartsWithSegments("/_framework") ||
            context.Request.Method == "OPTIONS")
        {
            return true;
        }

        var path = context.Request.Path.Value;
        if (string.IsNullOrEmpty(path))
            return false;

        foreach (var ext in StaticFileExtensions)
        {
            if (path.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
