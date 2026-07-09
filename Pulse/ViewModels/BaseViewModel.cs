using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pulse.Services;
using Pulse.Utils;

namespace Pulse.ViewModels;

public partial class BaseViewModel : ObservableObject
{
    protected readonly INavigationService? NavigationService;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _title = string.Empty;

    // Costruttore senza parametri (per compatibilità)
    public BaseViewModel()
    {
    }

    // Costruttore con NavigationService
    public BaseViewModel(INavigationService navigationService)
    {
        NavigationService = navigationService;
    }

    // ================================================
    // METODO PER CARICAMENTO DATI
    // ================================================

    protected async Task EseguiConCaricamento(Func<Task> action)
    {
        if (IsBusy)
        {
            System.Diagnostics.Debug.WriteLine("⚠️ EseguiConCaricamento: IsBusy è già TRUE, salto l'azione.");
            return;
        }

        try
        {
            IsBusy = true;
            await action();
        }
        catch (Exception ex)
        {
            await AlertPopup.ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ================================================
    // METODI PER NAVIGARE CON CARICAMENTO
    // ================================================

    protected async Task NavigaConCaricamento(string route, bool animate = true)
    {
        if (NavigationService == null)
        {
            System.Diagnostics.Debug.WriteLine("⚠️ NavigaConCaricamento: NavigationService non inizializzato!");
            return;
        }

        await EseguiConCaricamento(async () =>
            await NavigationService.NavigateToAsync(route, animate));
    }

    protected async Task NavigaConCaricamento(string route, Dictionary<string, object> parameters, bool animate = true)
    {
        if (NavigationService == null)
        {
            System.Diagnostics.Debug.WriteLine("⚠️ NavigaConCaricamento: NavigationService non inizializzato!");
            return;
        }

        await EseguiConCaricamento(async () =>
            await NavigationService.NavigateToAsync(route, parameters, animate));
    }

    protected async Task NavigaIndietroConCaricamento(bool animate = true)
    {
        if (NavigationService == null)
        {
            System.Diagnostics.Debug.WriteLine("⚠️ NavigaIndietroConCaricamento: NavigationService non inizializzato!");
            return;
        }

        await EseguiConCaricamento(async () =>
            await NavigationService.NavigateBackAsync(animate));
    }

    protected async Task NavigaAllaRootConCaricamento(bool animate = true)
    {
        if (NavigationService == null)
        {
            System.Diagnostics.Debug.WriteLine("⚠️ NavigaAllaRootConCaricamento: NavigationService non inizializzato!");
            return;
        }

        await EseguiConCaricamento(async () =>
            await NavigationService.NavigateBackToRootAsync(animate));
    }
}

/* ESEMPIO DI UTILIZZO
 * 
 * // 1. Nel tuo ViewModel specifico, inietta il servizio
 * public partial class MainViewModel : BaseViewModel
 * {
 *     public MainViewModel(INavigationService navigationService) : base(navigationService)
 *     {
 *     }
 * 
 *     [RelayCommand]
 *     private async Task VaiAlDettaglio()
 *     {
 *         // Navigazione con caricamento automatico
 *         await NavigaConCaricamento("dettaglio");
 *     }
 * 
 *     [RelayCommand]
 *     private async Task VaiAlDettaglioConParametri()
 *     {
 *         var parameters = new Dictionary<string, object>
 *         {
 *             { "id", 123 },
 *             { "nome", "Mario" }
 *         };
 *         await NavigaConCaricamento("dettaglio", parameters);
 *     }
 * 
 *     [RelayCommand]
 *     private async Task TornaIndietro()
 *     {
 *         await NavigaIndietroConCaricamento();
 *     }
 * 
 *     [RelayCommand]
 *     private async Task CaricaDati()
 *     {
 *         await EseguiConCaricamento(async () =>
 *         {
 *             // Qui la tua logica di caricamento dati
 *             await Task.Delay(1000);
 *             await AlertPopup.Show("Dati caricati!");
 *         });
 *     }
 * }
 */