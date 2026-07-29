using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Pulse.Helpers;
using Pulse.Models;
using Pulse.DTO;
using Pulse.Services;
using Pulse.Utils;
using Pulse.Views.Popups;

namespace Pulse.ViewModels;

public partial class CalendarioViewModel : BaseViewModel
{
    private readonly IDatabaseService _databaseService;

    // Messaggio per notificare la View di ridisegnare la griglia
    public class RefreshGridMessage { }

    [ObservableProperty] private List<Lezioni> _lezioniSettimana = new();
    [ObservableProperty] private DateTime _settimanaCorrente = DateTime.Now;
    [ObservableProperty] private TimeSpan _oraInizio = new(10, 0, 0);
    [ObservableProperty] private TimeSpan _oraFine = new(24, 0, 0);
    [ObservableProperty] private int _intervalloMinuti = 30;
    [ObservableProperty] private List<FasciaOraria> _fasceOrarie = new();
    [ObservableProperty] private bool _mostraSoloAttive = false;

    public List<int> IntervalliDisponibili { get; } = new() { 15, 30, 60 };

    // Aggiungi questo nella tua classe CalendarioViewModel
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

    public CalendarioViewModel(INavigationService navigationService, IDatabaseService databaseService)
        : base(navigationService)
    {
        _databaseService = databaseService;
        Title = "Calendario";
        GeneraFasceOrarie();
    }

    // Quando cambiano i filtri, aggiorniamo e inviamo il messaggio
    partial void OnIntervalloMinutiChanged(int value) => 
        AggiornaVista();
    partial void OnMostraSoloAttiveChanged(bool value) =>
        AggiornaVista();

    private void AggiornaVista()
    {
        GeneraFasceOrarie();
        WeakReferenceMessenger.Default.Send(new RefreshGridMessage());
    }

    private void GeneraFasceOrarie()
    {
        var lista = new List<FasciaOraria>();
        var ora = OraInizio;
        while (ora < OraFine)
        {
            bool attiva = !(ora >= new TimeSpan(13, 0, 0) && ora < new TimeSpan(14, 0, 0));
            lista.Add(new FasciaOraria { Orario = ora, IsAttiva = attiva });
            ora = ora.Add(TimeSpan.FromMinutes(IntervalloMinuti));
        }
        FasceOrarie = MostraSoloAttive ? lista.Where(f => f.IsAttiva).ToList() : lista;
    }

    public async Task LoadData()
    {
        await EseguiConCaricamento(async () =>
        {
            LezioniSettimana = await _databaseService.GetLezioniSettimana(SettimanaCorrente);
            WeakReferenceMessenger.Default.Send(new RefreshGridMessage());
        });
    }

    [RelayCommand] private async Task SettimanaPrecedente() { SettimanaCorrente = SettimanaCorrente.AddDays(-7); await LoadData(); }
    [RelayCommand] private async Task SettimanaSuccessiva() { SettimanaCorrente = SettimanaCorrente.AddDays(7); await LoadData(); }
    [RelayCommand] private async Task VaiOggi() { SettimanaCorrente = DateTime.Now; await LoadData(); }

    [RelayCommand]
    public async Task MostraAllieviCorso(Lezioni lezione)
    {
        if (lezione == null) return;

        await EseguiConCaricamento(async () => 
            await Shell.Current.Navigation.PushAsync(new AllieviCorsoPage(lezione))); // PUSH NORMALE
    }

    public TimeSpan StringToTimeSpan(string timeString) => 
        TimeSpan.TryParse(timeString, out var t) ? t : TimeSpan.Zero;
    public Color StringToColor(string colorString) => 
        Color.TryParse(colorString, out var c) ? c : Colors.Purple;

    public List<Lezioni> GetLezioniPerGiorno(DayOfWeek giorno) =>
        LezioniSettimana.Where(l => (DayOfWeek)l.GiornoSettimana == giorno).ToList();

    public Lezioni? GetLezionePerOrario(DayOfWeek giorno, TimeSpan orario)
    {
        // Convertiamo il DayOfWeek in intero (0-6) e poi lo adattiamo al formato DB (1-7)
        // Se la Domenica è 0, la portiamo a 7 per il confronto col DB
        int giornoDb = (int)giorno == 0 ? 7 : (int)giorno;

        return LezioniSettimana.FirstOrDefault(l =>
            l.GiornoSettimana == giornoDb && // Confrontiamo direttamente gli interi
            StringToTimeSpan(l.OraInizio) <= orario &&
            StringToTimeSpan(l.OraFine) > orario);
    }

    public string GetNomeGiorno(DayOfWeek giorno) => 
        DateHelper.GetNomeGiornoCompletoIT(giorno, SettimanaCorrente);

    // Passa la lezione intera alla pagina, così la pagina può accedere 
    // a Maestri, Allievi, Orari, ecc.
    [RelayCommand]
    public async Task GestisciLezione(Lezioni lezione) =>
        await Shell.Current.Navigation.PushAsync(new AllieviCorsoPage(lezione));
    

    public async Task CreaNuovaLezione(DayOfWeek giorno, TimeSpan ora)
    {
        // Creiamo una lezione "vuota" con le info base
        var nuovaLezione = new Lezioni
        {
            GiornoSettimana = (int)giorno,
            OraInizio = ora.ToString(@"hh\:mm"),
            OraFine = ora.Add(TimeSpan.FromMinutes(IntervalloMinuti)).ToString(@"hh\:mm")
        };

        // Per ora naviga verso la pagina, poi creeremo la logica di salvataggio
        await Shell.Current.Navigation.PushAsync(new AllieviCorsoPage(nuovaLezione));
    }

    // menu di navigazione 

    [RelayCommand]
    public async Task NavigaGestioneCorsi() => 
        await Shell.Current.GoToAsync(AppRoutes.Corsi.PaginaCorsi);

    [RelayCommand]
    public async Task NavigaGestioneInsegnanti() =>
        await Shell.Current.GoToAsync(AppRoutes.Insegnanti.PaginaInsegnanti);

}