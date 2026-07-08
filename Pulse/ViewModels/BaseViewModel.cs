using CommunityToolkit.Mvvm.ComponentModel;
using Pulse.Services;
using Pulse.Utils;

namespace Pulse.ViewModels;

public partial class BaseViewModel : ObservableObject
{
    protected readonly INavigationService NavigationService;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _title;

    public BaseViewModel()
    {
        // Se vuoi usare il servizio di navigazione, 
        // il costruttore senza parametri è per compatibilità
    }

    public BaseViewModel(INavigationService navigationService)
    {
        NavigationService = navigationService;
    }

    // Metodo caricamento pagina
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

    // Metodo per navigare con caricamento
    protected async Task NavigaConCaricamento(string route, bool animate = true)
    {
        await EseguiConCaricamento(async () =>
        {
            await NavigationService.NavigateToAsync(route, animate);
        });
    }

    protected async Task NavigaConCaricamento(string route, Dictionary<string, object> parameters, bool animate = true)
    {
        await EseguiConCaricamento(async () =>
        {
            await NavigationService.NavigateToAsync(route, parameters, animate);
        });
    }

    protected async Task NavigaIndietroConCaricamento(bool animate = true)
    {
        await EseguiConCaricamento(async () =>
        {
            await NavigationService.NavigateBackAsync(animate);
        });
    }

    protected async Task NavigaAllaRootConCaricamento(bool animate = true)
    {
        await EseguiConCaricamento(async () =>
        {
            await NavigationService.NavigateBackToRootAsync(animate);
        });
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