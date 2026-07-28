// ViewModels/GestioneCorsoViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pulse.Models;
using Pulse.Services;

namespace Pulse.ViewModels;

[QueryProperty(nameof(Corso), "Corso")]
public partial class GestioneCorsoViewModel : ObservableObject
{
    private readonly IDatabaseService _dbService;

    [ObservableProperty]
    private Corsi _corso = new();

    [ObservableProperty]
    private string _nome = string.Empty;

    [ObservableProperty]
    private string _descrizione = string.Empty;

    [ObservableProperty]
    private string _coloreHex = "#4F46E5";

    public GestioneCorsoViewModel(IDatabaseService dbService)
    {
        _dbService = dbService;
    }

    partial void OnCorsoChanged(Corsi value)
    {
        if (value == null) return;

        Nome = value.Nome ?? string.Empty;
        Descrizione = value.Descrizione ?? string.Empty;
        ColoreHex = string.IsNullOrWhiteSpace(value.Colore) ? "#4F46E5" : value.Colore;
    }

    [RelayCommand]
    public void SelezionaColorePreset(string hex)
    {
        if (!string.IsNullOrWhiteSpace(hex))
        {
            ColoreHex = hex;
        }
    }

    [RelayCommand]
    public async Task SalvaCorsoAsync()
    {
        if (string.IsNullOrWhiteSpace(Nome))
        {
            await Shell.Current.DisplayAlert("Attenzione", "Inserisci il nome del corso.", "OK");
            return;
        }

        Corso.Nome = Nome.Trim();
        Corso.Descrizione = Descrizione?.Trim();
        Corso.Colore = string.IsNullOrWhiteSpace(ColoreHex) ? "#4F46E5" : ColoreHex.Trim();

        await _dbService.SalvaCorsoAsync(Corso);
        await Shell.Current.Navigation.PopAsync();
    }
}