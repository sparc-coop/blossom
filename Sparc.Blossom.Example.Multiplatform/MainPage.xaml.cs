using Microsoft.AspNetCore.Components.WebView.Maui;
using Sparc.Blossom.Example.Multiplatform.Components.Layout;

namespace Sparc.Blossom.Example.Multiplatform
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

            var myAppAssembly = typeof(MainPage).Assembly;

            blazorWebView.RootComponents.Add(new RootComponent
            {
                Selector = "#app",
                ComponentType = typeof(BlossomApp),
                Parameters = new Dictionary<string, object>
                {
                    [nameof(BlossomApp.ProgramType)] = typeof(MauiProgram),

                    [nameof(BlossomApp.LayoutType)] = typeof(MainLayout),

                    [nameof(BlossomApp.ExtraAssemblies)] = new[]
                    {
                    myAppAssembly
                }
                }
            });
        }
    }
}
