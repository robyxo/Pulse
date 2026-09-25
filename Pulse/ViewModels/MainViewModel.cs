using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pulse.Services;
using Pulse.Utils;

namespace Pulse.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    private readonly IImpostazioniService _impostazioniService;
    private readonly IAggiornamentoService _aggiornamentoService;

    // Il controllo si fa una volta per avvio del programma, non a ogni ritorno alla Home.
    // Static perché MainViewModel è transient.
    private static bool _controlloAggiornamentiFatto;

    [ObservableProperty]
    private string _messaggioBenvenuto = "Ciao! Benvenuto in Pulse";

    [ObservableProperty]
    private string _sottotitolo = "La tua applicazione è pronta";

    [ObservableProperty]
    private ImageSource? _logoScuola;

    [ObservableProperty]
    private bool _mostraLogo;

    [ObservableProperty]
    private bool _mostraEmojiDefault = true;

    public MainViewModel(IImpostazioniService impostazioniService, IAggiornamentoService aggiornamentoService)
    {
        _impostazioniService = impostazioniService;
        _aggiornamentoService = aggiornamentoService;
        Title = "Home";
    }

    public async Task CaricaIntestazioneAsync()
    {
        var impostazioni = await _impostazioniService.GetImpostazioniAsync();

        MessaggioBenvenuto = string.IsNullOrWhiteSpace(impostazioni.NomeScuola)
            ? "Ciao! Benvenuto in Pulse"
            : impostazioni.NomeScuola;

        if (!string.IsNullOrWhiteSpace(impostazioni.LogoPath) && File.Exists(impostazioni.LogoPath))
        {
            LogoScuola = ImageSource.FromFile(impostazioni.LogoPath);
            MostraLogo = true;
            MostraEmojiDefault = false;
        }
        else
        {
            LogoScuola = null;
            MostraLogo = false;
            MostraEmojiDefault = true;
        }
    }

    /// <summary>
    /// All'apertura di Pulse: se su GitHub c'è una versione nuova, lo dice e porta
    /// alle Impostazioni. Ricompare a ogni avvio finché non si aggiorna.
    /// Silenzioso se non c'è internet, se è già aggiornato o se si avvia da
    /// Visual Studio (Pulse non installato con il Setup).
    /// </summary>
    public async Task ControllaAggiornamentiAllAvvioAsync()
    {
        if (_controlloAggiornamentiFatto) return;
        _controlloAggiornamentiFatto = true;

        if (!_aggiornamentoService.InstallazioneGestita) return;

        try
        {
            var (ceUnAggiornamento, nuovaVersione, _) = await _aggiornamentoService.ControllaAggiornamentiAsync();
            if (!ceUnAggiornamento) return;

            string versione = string.IsNullOrWhiteSpace(nuovaVersione) ? string.Empty : $" ({nuovaVersione})";

            bool vaiAlleImpostazioni = await AlertPopup.ShowConfirmation(
                "Aggiornamento disponibile",
                $"È disponibile una nuova versione di Pulse{versione}.\n\nVai in Impostazioni > Aggiornamenti per aggiornare.",
                "Vai alle Impostazioni",
                "Più tardi");

            if (vaiAlleImpostazioni)
                await Shell.Current.GoToAsync(AppRoutes.Impostazioni.Pagina);
        }
        catch
        {
            // Un controllo fallito non deve mai disturbare l'avvio.
        }
    }

    [RelayCommand]
    private async Task VaiAlCalendario() =>
        await EseguiConCaricamento(async () =>
            await Shell.Current.GoToAsync(AppRoutes.Calendario.Pagina));

    [RelayCommand]
    private async Task VaiAlleStatistiche() =>
        await EseguiConCaricamento(async () =>
            await Shell.Current.GoToAsync(AppRoutes.Statistiche.Pagina));
}