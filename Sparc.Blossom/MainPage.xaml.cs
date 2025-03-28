using Microsoft.AspNetCore.Components.WebView.Maui;

namespace Sparc.Blossom
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

            blazorWebView.RootComponents.Add(new RootComponent
            {
                Selector = "#app",
                ComponentType = typeof(BlossomApp),
                Parameters = new Dictionary<string, object>
                    {
                        { nameof(BlossomApp.ProgramType), typeof(MauiProgram) },
                        { nameof(BlossomApp.LayoutType), MauiProgram.LayoutType }
                    }
            });
        }
    }
}
