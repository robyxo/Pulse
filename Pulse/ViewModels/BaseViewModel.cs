using CommunityToolkit.Mvvm.ComponentModel;
using Pulse.Utils;

namespace Pulse.ViewModels;

public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _title = string.Empty;

    // ================================================
    // METODI PER CARICAMENTO DATI
    // ================================================

    /// <summary>
    /// Esegue l'azione gestendo gli errori, SENZA toccare IsBusy.
    /// Da usare quando la chiamata avviene già dentro un EseguiConCaricamento:
    /// in quel caso il guard su IsBusy farebbe saltare l'azione in silenzio.
    /// </summary>
    protected async Task EseguiSempre(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            await AlertPopup.ShowError(ex.Message);
        }
    }

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
            await EseguiSempre(action);
        }
        finally
        {
            IsBusy = false;
        }
    }
}