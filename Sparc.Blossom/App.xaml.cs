using Microsoft.Maui.Controls;

namespace Sparc.Blossom
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new MainPage()) { Title = "Sparc.Blossom" };
        }
    }
}
