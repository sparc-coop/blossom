namespace Sparc.Blossom;

public partial class BlossomMauiApp : Microsoft.Maui.Controls.Application
{
    public BlossomMauiApp()
    {
        InitializeComponent();

        MainPage = new BlossomMainPage();
    }
}
