using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pulse.Services;
using Pulse.Utils;

namespace Pulse.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    [ObservableProperty]
    private string _messaggioBenvenuto = "Ciao! Benvenuto in Pulse";

    [ObservableProperty]
    private string _sottotitolo = "La tua applicazione è pronta";

    public MainViewModel(INavigationService navigationService) : base(navigationService)
    {
        Title = "Home";
    }

    // Esempio di comando per navigare a una pagina di dettaglio
    [RelayCommand]
    private async Task VaiAlDettaglio() => 
        await NavigaConCaricamento("dettaglio");
    

    // Esempio di comando con caricamento dati
    [RelayCommand]
    private async Task CaricaDati()
    {
        await EseguiConCaricamento(async () =>
        {
            // Simula caricamento dati
            await Task.Delay(1000);

            // Aggiorna il titolo dopo il caricamento
            Title = "Dati caricati!";

            await AlertPopup.Show("Dati caricati con successo!");
        });
    }

    // Esempio di navigazione con parametri
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
}