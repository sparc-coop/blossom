using Microsoft.Maui.Controls;

namespace Sparc.Blossom
{
    public partial class MobileApp : Application
    {
        public MobileApp()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new MainPage()) { Title = "Sparc.Blossom" };
        }
    }
}
