using Pulse.Utils;

namespace Pulse;

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
        var routes = new Dictionary<string, Type>
        {
            { AppRoutes.Calendario.Pagina, typeof(CalendarioPage) },
            
            // Rotte Corsi
            { AppRoutes.Corsi.PaginaCorsi, typeof(CorsiPage) },
            { AppRoutes.Corsi.GestioneCorso, typeof(GestioneCorsoPage) },

            // 👨‍🏫 Rotte Insegnanti
            { AppRoutes.Insegnanti.PaginaInsegnanti, typeof(InsegnantiPage) },
            { AppRoutes.Insegnanti.GestioneInsegnante, typeof(GestioneInsegnantePage) }
        };

        foreach (var item in routes)
        {
            Routing.RegisterRoute(item.Key, item.Value);
        }

#if DEBUG
        // rotte per versione debug
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