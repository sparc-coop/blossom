using Microsoft.Maui.Controls;

namespace Sparc.Blossom
{
    public partial class BlossomApp : Application
    {
        public BlossomApp()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new BlossomMainPage()) { Title = "Sparc.Blossom" };
        }
    }
}
