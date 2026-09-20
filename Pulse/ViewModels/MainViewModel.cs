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

    public MainViewModel(IImpostazioniService impostazioniService)
    {
        _impostazioniService = impostazioniService;
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

    [RelayCommand]
    private async Task VaiAlCalendario() =>
        await EseguiConCaricamento(async () =>
            await Shell.Current.GoToAsync(AppRoutes.Calendario.Pagina));
}