namespace Sparc.Blossom;

public class BlossomApplication
{
    public static IBlossomApplicationBuilder CreateBuilder(string[]? args = null)
    {
#if BROWSER
        return new Platforms.Browser.BlossomBrowserApplicationBuilder(args);
#elif SERVER
        return new Platforms.Server.BlossomServerApplicationBuilder(args ?? []);
#endif
        throw new NotImplementedException();
    }
}
