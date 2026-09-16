using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pulse.Services;
using Pulse.Utils;

namespace Pulse.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    private readonly IImpostazioniService _impostazioniService;

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

    private readonly INavigationService _navigationService;

    public MainViewModel(INavigationService navigationService, IImpostazioniService impostazioniService) : base(navigationService)
    {
        _navigationService = navigationService;
        _impostazioniService = impostazioniService;
        Title = "Home";

        _ = CaricaIntestazioneAsync();
    }

    [RelayCommand]
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

    [RelayCommand]
    private async Task VaiAlDettaglio() =>
        await NavigaConCaricamento("dettaglio");

    [RelayCommand]
    private async Task CaricaDati()
    {
        await EseguiConCaricamento(async () =>
        {
            await Task.Delay(1000);
            Title = "Dati caricati!";
            await AlertPopup.Show("Dati caricati con successo!");
        });
    }

    [RelayCommand]
    private async Task VaiConParametri()
    {
        var parameters = new Dictionary<string, object>
        {
            { "id", 42 },
            { "nome", "Esempio" }
        };

        await NavigaConCaricamento("dettaglio", parameters);
    }

    [RelayCommand]
    private async Task VaiAlCalendario()
    {
        await _navigationService.GoToAsync("calendario");
    }
}