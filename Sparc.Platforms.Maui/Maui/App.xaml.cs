using Application = Microsoft.Maui.Controls.Application;

namespace Sparc.Blossom.Maui;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        MainPage = new MainPage();
    }
}