using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pulse.Models;
using Pulse.Services;
using Pulse.Utils;

namespace Pulse.ViewModels;

[QueryProperty(nameof(Corso), "Corso")]
public partial class GestioneCorsoViewModel : BaseViewModel
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

    // 💶 PROPRIETÀ PREZZI ABBONAMENTO
    [ObservableProperty]
    private double? _costoSingolo;

    [ObservableProperty]
    private double? _costoMensile;

    [ObservableProperty]
    private double? _costoAnnuale;

    [ObservableProperty]
    private bool _isEdizione = false;

    public GestioneCorsoViewModel(INavigationService navigationService, IDatabaseService dbService)
        : base(navigationService)
    {
        _dbService = dbService;
        Title = "Gestione Corso";
    }

    partial void OnCorsoChanged(Corsi value)
    {
        if (value == null) return;

        Nome = value.Nome ?? string.Empty;
        Descrizione = value.Descrizione ?? string.Empty;
        ColoreHex = string.IsNullOrWhiteSpace(value.Colore) ? "#4F46E5" : value.Colore;

        CostoSingolo = value.CostoSingolo;
        CostoMensile = value.CostoMensile;
        CostoAnnuale = value.CostoAnnuale;

        IsEdizione = value.Id > 0;
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

        await EseguiConCaricamento(async () =>
        {
            Corso ??= new Corsi();

            Corso.Nome = Nome.Trim();
            Corso.Descrizione = Descrizione?.Trim();
            Corso.Colore = string.IsNullOrWhiteSpace(ColoreHex) ? "#4F46E5" : ColoreHex.Trim();

            // Assegnazione Prezzi
            Corso.CostoSingolo = CostoSingolo ?? 0;
            Corso.CostoMensile = CostoMensile ?? 0;
            Corso.CostoAnnuale = CostoAnnuale ?? 0;

            await _dbService.SalvaCorsoAsync(Corso);
            await Shell.Current.Navigation.PopAsync();
        });
    }

    [RelayCommand]
    public async Task EliminaCorsoAsync()
    {
        if (Corso == null || Corso.Id == 0) return;

        bool conferma = await Shell.Current.DisplayAlert(
            "Conferma Eliminazione",
            $"Sei sicuro di voler eliminare il corso '{Corso.Nome}'?",
            "Sì, Elimina",
            "Annulla");

        if (conferma)
        {
            await EseguiConCaricamento(async () =>
            {
                await _dbService.EliminaCorsoAsync(Corso.Id);
                await Shell.Current.Navigation.PopAsync();
            });
        }
    }
}