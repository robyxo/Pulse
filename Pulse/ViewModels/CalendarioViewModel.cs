using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pulse.Helpers;
using Pulse.Models;
using Pulse.Services;
using Pulse.Utils;
using Pulse.Views.Popups;

namespace Pulse.ViewModels;

public partial class CalendarioViewModel : BaseViewModel
{
    private readonly IDatabaseService _databaseService;

    public class FasciaOraria
    {
        public TimeSpan Orario { get; set; }
        public bool IsAttiva { get; set; } = true;
    }

    [ObservableProperty] private List<Lezioni> _lezioniSettimana = new();
    [ObservableProperty] private DateTime _settimanaCorrente = DateTime.Now;
    [ObservableProperty] private TimeSpan _oraInizio = new(10, 0, 0);
    [ObservableProperty] private TimeSpan _oraFine = new(24, 0, 0);
    [ObservableProperty] private int _intervalloMinuti = 30;
    [ObservableProperty] private List<FasciaOraria> _fasceOrarie = new();
    [ObservableProperty] private bool _mostraSoloAttive = false;

    public List<int> IntervalliDisponibili { get; } = new() { 15, 30, 60 };
    public List<DayOfWeek> GiorniSettimana { get; } = new()
    {
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
        DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
    };

    public CalendarioViewModel(INavigationService navigationService, IDatabaseService databaseService)
        : base(navigationService)
    {
        _databaseService = databaseService;
        Title = "Calendario";
        GeneraFasceOrarie();
    }

    partial void OnIntervalloMinutiChanged(int value) => GeneraFasceOrarie();
    partial void OnOraInizioChanged(TimeSpan value) => GeneraFasceOrarie();
    partial void OnOraFineChanged(TimeSpan value) => GeneraFasceOrarie();

    private void GeneraFasceOrarie()
    {
        var lista = new List<FasciaOraria>();
        var ora = OraInizio;
        while (ora < OraFine)
        {
            // Esempio: pausa 13:00-14:00 disattivata
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
        {
            var allievi = await _databaseService.GetAllieviPerCorso(lezione.CorsoId);
            var nomeCorso = lezione.Corso?.Nome ?? $"Corso {lezione.CorsoId}";
            var page = new AllieviCorsoPage(nomeCorso, allievi);
            await Shell.Current.Navigation.PushAsync(page); // PUSH NORMALE
        });
    }

    public TimeSpan StringToTimeSpan(string timeString) => TimeSpan.TryParse(timeString, out var t) ? t : TimeSpan.Zero;
    public Color StringToColor(string colorString) => Color.TryParse(colorString, out var c) ? c : Colors.Purple;

    public List<Lezioni> GetLezioniPerGiorno(DayOfWeek giorno) =>
        LezioniSettimana.Where(l => (DayOfWeek)l.GiornoSettimana == giorno).ToList();

    public Lezioni? GetLezionePerOrario(DayOfWeek giorno, TimeSpan orario) =>
        LezioniSettimana.FirstOrDefault(l =>
            (DayOfWeek)l.GiornoSettimana == giorno &&
            StringToTimeSpan(l.OraInizio) <= orario &&
            StringToTimeSpan(l.OraFine) > orario);

    public string GetNomeGiorno(DayOfWeek giorno)
    {
        return DateHelper.GetNomeGiornoCompletoIT(giorno, SettimanaCorrente);
    }
}