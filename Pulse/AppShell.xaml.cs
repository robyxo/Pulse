using Pulse.Utils;

namespace Pulse
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            FlyoutBehavior = FlyoutBehavior.Disabled;
            RegisterRoutes();
            ConfiguraPaginaIniziale();
        }

        private void RegisterRoutes()
        {
            // ❌ ATTENZIONE: Non inserire QUI la Home e l'Accesso!
            // Essendo pagine "Radice" caricate dinamicamente giù, se le metti qui 
            // MAUI le apre come pagine scollegate.

            var routes = new Dictionary<string, Type>
            {
                // --- MAIN (Solo le pagine figlie) ---
                { AppRoutes.Main.MainPage, typeof(MainPage) },

                // --- ALTRE PAGINE ---

            };

            foreach (var item in routes)
            {
                Routing.RegisterRoute(item.Key, item.Value);
            }
#if DEBUG
            // rotte per veriosne debug

#endif
        }

        private void ConfiguraPaginaIniziale()
        {
            ShellContent mainContent = new ShellContent();

#if DEBUG
            // In Debug parte dalla MainPage
            mainContent.Title = "Home";
            mainContent.Route = AppRoutes.Main.MainPage;
            mainContent.ContentTemplate = new DataTemplate(typeof(MainPage));
#else
            // In Release parte dalla AccessoPage
            mainContent.Title = "Home";
            mainContent.Route = AppRoutes.Main.MainPage;
            mainContent.ContentTemplate = new DataTemplate(typeof(MainPage));
#endif
            Shell.SetNavBarIsVisible(mainContent, false);
            this.Items.Add(mainContent);
        }
    }
}
