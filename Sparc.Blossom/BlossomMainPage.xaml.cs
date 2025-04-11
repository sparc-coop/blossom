using Microsoft.AspNetCore.Components.WebView.Maui;

namespace Sparc.Blossom
{
    public partial class BlossomMainPage : ContentPage
    {
        public BlossomMainPage()
        {
            InitializeComponent();

            blazorWebView.RootComponents.Add(new RootComponent
            {
                Selector = "#app",
                //ComponentType = typeof(BlossomApp),
                //Parameters = new Dictionary<string, object>
                //    {
                //        { nameof(BlossomApp.ProgramType), typeof(MauiProgram) },
                //        { nameof(BlossomApp.LayoutType), MauiProgram.LayoutType }
                //    }
            });
        }
    }
}
