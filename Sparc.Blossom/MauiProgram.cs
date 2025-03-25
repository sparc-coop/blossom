using Microsoft.Extensions.Logging;

namespace Sparc.Blossom
{
    public static class MauiProgram
    {
        public static Type? LayoutType { get; set; }

        public static MauiApp CreateMauiApp()
        {
            //Program.Main([]);
#if ANDROID
            return Sparc.Blossom.Platforms.Android.BlossomAndroidApplication.MauiApp;
#elif WINDOWS
            return Sparc.Blossom.Platforms.Windows.BlossomWindowsApplication.MauiApp;
#elif IOS
            return Sparc.Blossom.Platforms.iOS.BlossomiOSApplication.MauiApp;
#endif
            throw new NotImplementedException();
        }
    }
}
