namespace Sparc.Blossom.Example.Multiplatform
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new MainPage()) { Title = "Sparc.Blossom.Example.Multiplatform" };
        }
    }
}
