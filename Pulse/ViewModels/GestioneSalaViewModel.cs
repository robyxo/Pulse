using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pulse.Models;
using Pulse.Services;

namespace Pulse.ViewModels;

[QueryProperty(nameof(Sala), "Sala")]
public partial class GestioneSalaViewModel : BaseViewModel
{
    private readonly IDatabaseService _dbService;

    [ObservableProperty]
    private Sale _sala = new();

    [ObservableProperty]
    private string _nome = string.Empty;

    [ObservableProperty]
    private string _descrizione = string.Empty;

    [ObservableProperty]
    private int _capienza;

    [ObservableProperty]
    private bool _isEdizione = false;

    public GestioneSalaViewModel(IDatabaseService dbService)
    {
        _dbService = dbService;
        Title = "Gestione Sala";
    }

    partial void OnSalaChanged(Sale value)
    {
        if (value == null) return;

        Nome = value.Nome ?? string.Empty;
        Descrizione = value.Descrizione ?? string.Empty;
        Capienza = value.Capienza ?? 0;

        IsEdizione = value.Id > 0;
    }

    [RelayCommand]
    public async Task SalvaSalaAsync()
    {
        if (string.IsNullOrWhiteSpace(Nome))
        {
            await Shell.Current.DisplayAlert("Attenzione", "Inserisci il nome della sala.", "OK");
            return;
        }

        await EseguiConCaricamento(async () =>
        {
            Sala ??= new Sale();

            Sala.Nome = Nome.Trim();
            Sala.Descrizione = string.IsNullOrWhiteSpace(Descrizione) ? null : Descrizione.Trim();
            Sala.Capienza = Capienza > 0 ? Capienza : 0;

            // Il colore non è ancora modificabile dall'interfaccia: si tiene
            // quello esistente, o il predefinito per le sale nuove.
            if (string.IsNullOrWhiteSpace(Sala.Colore)) Sala.Colore = "#0EA5E9";

            await _dbService.SalvaSalaAsync(Sala);
            await Shell.Current.Navigation.PopAsync();
        });
    }

    [RelayCommand]
    public async Task EliminaSalaAsync()
    {
        if (Sala == null || Sala.Id == 0) return;

        bool conferma = await Shell.Current.DisplayAlert(
            "Conferma Eliminazione",
            $"Sei sicuro di voler eliminare la sala '{Sala.Nome}'?\n\nLe lezioni che la usano restano al loro posto, ma senza sala assegnata.",
            "Sì, Elimina",
            "Annulla");

        if (conferma)
        {
            await EseguiConCaricamento(async () =>
            {
                await _dbService.EliminaSalaAsync(Sala.Id);
                await Shell.Current.Navigation.PopAsync();
            });
        }
    }
}
