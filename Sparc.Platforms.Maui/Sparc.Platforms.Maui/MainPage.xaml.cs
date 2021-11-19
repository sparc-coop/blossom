using Microsoft.Maui.Controls;
using Microsoft.AspNetCore.Components.WebView.Maui;
using System;

namespace Sparc.Platforms.Maui
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

            BlazorWebView webView = new()
            {
                HostPage = "wwwroot/index.html"
            };
            webView.RootComponents.Add(new RootComponent
            {
                Selector = "app",
                ComponentType = typeof(Main)
            });

            Content = webView;
        }
    }
}
