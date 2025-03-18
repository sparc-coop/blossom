using Microsoft.Extensions.Logging;

namespace Sparc.Blossom
{
    public static class MauiProgram
    {
        public static Type LayoutType { get; set; } = typeof(MainLayout);

        public static MauiApp CreateMauiApp()
        {
            //Program.Main([]);
#if ANDROID
            return Sparc.Blossom.Platforms.Android.BlossomAndroidApplication.MauiApp;
#endif
            throw new NotImplementedException();
        }
    }
}
