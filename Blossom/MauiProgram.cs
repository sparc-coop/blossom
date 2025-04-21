using Microsoft.Extensions.Logging;
using Sparc.Blossom.Platforms.Android;

namespace Blossom
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            Program.Main([]);

            return BlossomAndroidApplication.MauiApp;
        }
    }
}
