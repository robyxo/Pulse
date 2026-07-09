using CommunityToolkit.Mvvm.Input;
using Pulse.Models;
using Pulse.Services;
using Pulse.Utils;

namespace Pulse.ViewModels;

public partial class CalendarioViewModel : BaseViewModel
{
    private readonly IDatabaseService _databaseService;

    [ObservableProperty]
    private List<Lezioni> _lezioniSettimana = new();

    [ObservableProperty]
    private DateTime _settimanaCorrente = DateTime.Now;

    [ObservableProperty]
    private TimeSpan _oraInizio = new TimeSpan(10, 0, 0); // 10:00

    [ObservableProperty]
    private TimeSpan _oraFine = new TimeSpan(24, 0, 0); // 24:00

    [ObservableProperty]
    private int _intervalloMinuti = 30;

    [ObservableProperty]
    private bool _isCaricamento;

    // Lista giorni della settimana
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
    }

    public async Task LoadData()
    {
        await EseguiConCaricamento(async () =>
        {
            LezioniSettimana = await _databaseService.GetLezioniSettimana(SettimanaCorrente);
        });
    }

    // ================================================
    // COMANDI DI NAVIGAZIONE SETTIMANA
    // ================================================

    [RelayCommand]
    private async Task SettimanaPrecedente()
    {
        SettimanaCorrente = SettimanaCorrente.AddDays(-7);
        await LoadData();
    }

    [RelayCommand]
    private async Task SettimanaSuccessiva()
    {
        SettimanaCorrente = SettimanaCorrente.AddDays(7);
        await LoadData();
    }

    [RelayCommand]
    private async Task VaiOggi()
    {
        SettimanaCorrente = DateTime.Now;
        await LoadData();
    }

    // ================================================
    // MOSTRA ALLIEVI DEL CORSO
    // ================================================

    [RelayCommand]
    public async Task MostraAllieviCorso(Lezioni lezione)
    {
        await EseguiConCaricamento(async () =>
        {
            var allievi = await _databaseService.GetAllieviPerCorso(lezione.CorsoId);

            if (allievi.Count == 0)
            {
                await AlertPopup.Show(
                    $"👥 {lezione.Corso?.Nome ?? "Corso"}",
                    "Nessun allievo iscritto a questo corso."
                );
                return;
            }

            var messaggio = string.Join("\n", allievi.Select(a => $"• {a.Nome} {a.Cognome}"));
            await AlertPopup.Show(
                $"👥 {lezione.Corso?.Nome ?? "Corso"} ({allievi.Count} allievi)",
                messaggio
            );
        });
    }

    // ================================================
    // METODI DI UTILITÀ PER LA CONVERSIONE
    // ================================================

    // Converte string "HH:mm" in TimeSpan
    public TimeSpan StringToTimeSpan(string timeString)
    {
        if (string.IsNullOrEmpty(timeString)) return TimeSpan.Zero;
        try
        {
            return TimeSpan.Parse(timeString);
        }
        catch
        {
            return TimeSpan.Zero;
        }
    }

    // Converte TimeSpan in string "HH:mm"
    public string TimeSpanToString(TimeSpan time)
    {
        return time.ToString(@"hh\:mm");
    }

    // Converte string colore in Color
    public Color StringToColor(string colorString)
    {
        if (string.IsNullOrEmpty(colorString)) return Colors.Purple;
        try
        {
            return Color.FromArgb(colorString);
        }
        catch
        {
            return Colors.Purple;
        }
    }

    // ================================================
    // METODI PER LA UI
    // ================================================

    // Ottiene le lezioni per un giorno specifico
    public List<Lezioni> GetLezioniPerGiorno(DayOfWeek giorno)
    {
        return LezioniSettimana
            .Where(l => (DayOfWeek)l.GiornoSettimana == giorno)
            .OrderBy(l => l.OraInizio)
            .ToList();
    }

    // Controlla se c'è una lezione in un determinato orario
    public Lezioni? GetLezionePerOrario(DayOfWeek giorno, TimeSpan orario)
    {
        return LezioniSettimana.FirstOrDefault(l =>
            (DayOfWeek)l.GiornoSettimana == giorno &&
            StringToTimeSpan(l.OraInizio) <= orario &&
            StringToTimeSpan(l.OraFine) > orario);
    }

    // Ottiene il nome del giorno
    public string GetNomeGiorno(DayOfWeek giorno, DateTime dataRiferimento)
    {
        var data = dataRiferimento.Date;
        while (data.DayOfWeek != DayOfWeek.Monday)
        {
            data = data.AddDays(-1);
        }
        data = data.AddDays((int)giorno - (int)DayOfWeek.Monday);

        return $"{giorno.ToString().Substring(0, 3)} {data.Day}";
    }
}