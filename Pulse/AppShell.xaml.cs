using Pulse.Utils;
using Pulse.Views;

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
            // 📅 Rotte Calendario & Lezioni
            { AppRoutes.Calendario.Pagina, typeof(CalendarioPage) },
            { AppRoutes.Calendario.GestioneLezione, typeof(GestioneLezionePage) },
            
            // 📚 Rotte Corsi
            { AppRoutes.Corsi.PaginaCorsi, typeof(CorsiPage) },
            { AppRoutes.Corsi.GestioneCorso, typeof(GestioneCorsoPage) },

            // 👨‍🏫 Rotte Insegnanti
            { AppRoutes.Insegnanti.PaginaInsegnanti, typeof(InsegnantiPage) },
            { AppRoutes.Insegnanti.GestioneInsegnante, typeof(GestioneInsegnantePage) },

            // 👨‍🎓 Rotte Allievi
            { AppRoutes.Allievi.PaginaAllievi, typeof(AllieviPage) },
            { AppRoutes.Allievi.GestioneAllievo, typeof(GestioneAllievoPage) },

             // 📨 Rotte Notifiche & Impostazioni
            { AppRoutes.Notifiche.Pagina, typeof(NotifichePage) },
            { AppRoutes.Impostazioni.Pagina, typeof(ImpostazioniPage) }
        };

        foreach (var item in routes)
        {
            Routing.RegisterRoute(item.Key, item.Value);
        }
    }

    private void ConfiguraPaginaIniziale()
    {
        ShellContent mainContent = new ShellContent
        {
            Title = "Home",
            Route = AppRoutes.Main.MainPage,
            ContentTemplate = new DataTemplate(typeof(MainPage))
        };

        Shell.SetNavBarIsVisible(mainContent, false);
        this.Items.Add(mainContent);
    }
}