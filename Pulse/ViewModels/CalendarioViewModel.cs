using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Pulse.Helpers;
using Pulse.Models;
using Pulse.DTO;
using Pulse.Services;
using Pulse.Utils;

namespace Pulse.ViewModels;

public partial class CalendarioViewModel : BaseViewModel
{
    private readonly IDatabaseService _databaseService;
    private readonly IImpostazioniService _impostazioniService;

    // Messaggio per notificare la View di ridisegnare la griglia
    public class RefreshGridMessage { }

    [ObservableProperty] private List<Lezioni> _lezioniSettimana = new();
    [ObservableProperty] private TimeSpan _oraInizio = new(8, 0, 0);
    [ObservableProperty] private TimeSpan _oraFine = new(24, 0, 0);
    [ObservableProperty] private int _intervalloMinuti = 30;
    [ObservableProperty] private List<TimeSpan> _slotOrari = new();
    [ObservableProperty] private bool _orarioScaglionatoAttivo;
    [ObservableProperty] private OpzioneIntervallo? _intervalloSelezionato;

    // Colori dei rettangoli delle lezioni: quello del corso (default) o quello della sala.
    public const string ColoriPerCorsi = "Corsi";
    public const string ColoriPerSale = "Sale";

    public List<string> OpzioniColori { get; } = new() { ColoriPerCorsi, ColoriPerSale };

    [ObservableProperty] private string _coloriPer = ColoriPerCorsi;

    public bool ColoriPerSala => ColoriPer == ColoriPerSale;

    public List<OpzioneIntervallo> OpzioniIntervallo { get; } = new()
    {
        new OpzioneIntervallo { Minuti = 30, Etichetta = "30 minuti" },
        new OpzioneIntervallo { Minuti = 60, Etichetta = "1 ora" },
        new OpzioneIntervallo { Minuti = 90, Etichetta = "1 ora e 30" },
        new OpzioneIntervallo { Minuti = 120, Etichetta = "2 ore" }
    };

    public List<DayOfWeek> GiorniSettimana { get; } = new()
    {
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday,
        DayOfWeek.Saturday,
        DayOfWeek.Sunday
    };

    public CalendarioViewModel(IDatabaseService databaseService, IImpostazioniService impostazioniService)
    {
        _databaseService = databaseService;
        _impostazioniService = impostazioniService;
        Title = "Calendario";

        // Ripristina gli ultimi filtri usati (Dalle / Alle / Intervallo)
        if (TimeSpan.TryParse(Preferences.Get("Calendario_OraInizio", string.Empty), out var oraInizioSalvata))
            OraInizio = oraInizioSalvata;

        if (TimeSpan.TryParse(Preferences.Get("Calendario_OraFine", string.Empty), out var oraFineSalvata))
            OraFine = oraFineSalvata;

        IntervalloMinuti = Preferences.Get("Calendario_IntervalloMinuti", IntervalloMinuti);

        IntervalloSelezionato = OpzioniIntervallo.FirstOrDefault(o => o.Minuti == IntervalloMinuti) ?? OpzioniIntervallo.First();

        // Ultima scelta dei colori, se valida; altrimenti per corso.
        string coloriSalvati = Preferences.Get("Calendario_ColoriPer", ColoriPerCorsi);
        ColoriPer = OpzioniColori.Contains(coloriSalvati) ? coloriSalvati : ColoriPerCorsi;

        GeneraSlotOrari();
    }

    // Quando cambiano i filtri, aggiorniamo e inviamo il messaggio
    // Quando cambiano i filtri, salviamo e aggiorniamo la vista
    partial void OnIntervalloMinutiChanged(int value)
    {
        Preferences.Set("Calendario_IntervalloMinuti", value);
        AggiornaVista();
    }

    partial void OnOraInizioChanged(TimeSpan value)
    {
        Preferences.Set("Calendario_OraInizio", value.ToString());
        AggiornaVista();
    }

    partial void OnOraFineChanged(TimeSpan value)
    {
        Preferences.Set("Calendario_OraFine", value.ToString());
        AggiornaVista();
    }

    partial void OnColoriPerChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        Preferences.Set("Calendario_ColoriPer", value);
        WeakReferenceMessenger.Default.Send(new RefreshGridMessage());
    }

    partial void OnIntervalloSelezionatoChanged(OpzioneIntervallo? value)
    {
        if (value == null) return;
        IntervalloMinuti = value.Minuti; // scatena già OnIntervalloMinutiChanged -> AggiornaVista
    }

    private void AggiornaVista()
    {
        GeneraSlotOrari();
        WeakReferenceMessenger.Default.Send(new RefreshGridMessage());
    }

    private void GeneraSlotOrari()
    {
        var lista = new List<TimeSpan>();
        var ora = OraInizio;
        while (ora < OraFine)
        {
            lista.Add(ora);
            ora = ora.Add(TimeSpan.FromMinutes(IntervalloMinuti));
        }
        SlotOrari = lista;
    }

    public async Task CaricaDatiAsync()
    {
        await EseguiConCaricamento(async () =>
        {
            var impostazioni = await _impostazioniService.GetImpostazioniAsync();
            OrarioScaglionatoAttivo = impostazioni.OrarioScaglionato == 1;

            LezioniSettimana = await _databaseService.GetLezioniRicorrentiAsync();
            WeakReferenceMessenger.Default.Send(new RefreshGridMessage());
        });
    }

    public Color StringToColor(string colorString) => 
        Color.TryParse(colorString, out var c) ? c : Colors.Purple;

    // Solo il nome del giorno: senza settimane, la data non ha piu' senso.
    public string GetNomeGiorno(DayOfWeek giorno) =>
        DateHelper.GetNomeGiornoDaDb((int)giorno == 0 ? 7 : (int)giorno);

    /// <summary>Scritta scura su colori chiari, bianca su quelli scuri.</summary>
    public static Color ColoreTestoLeggibile(Color sfondo)
    {
        double luminosita = 0.299 * sfondo.Red + 0.587 * sfondo.Green + 0.114 * sfondo.Blue;
        return luminosita > 0.6 ? Color.FromArgb("#1E293B") : Colors.White;
    }

    // Passa la lezione intera alla pagina, così la pagina può accedere 
    // a Maestri, Allievi, Orari, ecc.
    [RelayCommand]
    public async Task GestisciLezione(Lezioni lezione)
    {
        if (lezione == null) return;
        var parametri = new Dictionary<string, object> { { "Lezione", lezione } };
        await Shell.Current.GoToAsync(AppRoutes.Calendario.GestioneLezione, parametri);
    }

    public async Task CreaNuovaLezione(DayOfWeek giorno, TimeSpan ora)
    {
        int giornoDb = (int)giorno == 0 ? 7 : (int)giorno;

        var nuovaLezione = new Lezioni
        {
            GiornoSettimana = giornoDb,
            OraInizio = ora.ToString(@"hh\:mm"),
            OraFine = ora.Add(TimeSpan.FromMinutes(IntervalloMinuti)).ToString(@"hh\:mm")
        };

        var parametri = new Dictionary<string, object> { { "Lezione", nuovaLezione } };
        await Shell.Current.GoToAsync(AppRoutes.Calendario.GestioneLezione, parametri);
    }

    // menu di navigazione 

    [RelayCommand]
    public async Task NavigaGestioneCorsi() => 
        await Shell.Current.GoToAsync(AppRoutes.Corsi.PaginaCorsi);

    [RelayCommand]
    public async Task NavigaGestioneInsegnanti() =>
        await Shell.Current.GoToAsync(AppRoutes.Insegnanti.PaginaInsegnanti);

    [RelayCommand]
    public async Task NavigaGestioneAllievi() =>
        await Shell.Current.GoToAsync(AppRoutes.Allievi.PaginaAllievi);

    [RelayCommand]
    public async Task NavigaGestioneSale() =>
        await Shell.Current.GoToAsync(AppRoutes.Sale.PaginaSale);

    [RelayCommand]
    public async Task NavigaNotifiche() =>
        await Shell.Current.GoToAsync(AppRoutes.Notifiche.Pagina);

    [RelayCommand]
    public async Task NavigaImpostazioni() =>
        await Shell.Current.GoToAsync(AppRoutes.Impostazioni.Pagina);

    [RelayCommand]
    public async Task NavigaCalendarioEventi() =>
    await Shell.Current.GoToAsync(AppRoutes.Calendario.Eventi);

}