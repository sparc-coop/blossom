using Microsoft.AspNetCore.Components.WebView.Maui;

namespace TemplateMAUINET7.MAUI
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

            //BlazorWebView webView = new()
            //{
            //    HostPage = "wwwroot/index.html"
            //};
            //webView.RootComponents.Add(new RootComponent
            //{
            //    Selector = "app",
            //    ComponentType = typeof(Main)
            //});

            //Content = webView;
        }
    }
}