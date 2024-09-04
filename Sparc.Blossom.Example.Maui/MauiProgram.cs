using Microsoft.Extensions.Logging;
using Sparc.Blossom;

namespace Sparc.Blossom.Example.Maui
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            return BlossomApplication.CreateMauiApp<App>();
        }
    }
}
