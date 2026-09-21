namespace Pulse
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Pulse usa SEMPRE il tema chiaro, indipendentemente da Windows.
            // I colori delle pagine sono fissi e pensati per sfondo chiaro:
            // con il tema scuro alcune scritte diventerebbero illeggibili.
            UserAppTheme = AppTheme.Light;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}