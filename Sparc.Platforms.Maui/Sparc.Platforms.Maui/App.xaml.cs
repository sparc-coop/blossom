using Application = Microsoft.Maui.Controls.Application;

namespace Sparc.Platforms.Maui
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new MainPage();
        }
    }
}