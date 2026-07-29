using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pulse.Models;
using Pulse.Services;

namespace Pulse.ViewModels;

[QueryProperty(nameof(Insegnante), "Insegnante")]
public partial class GestioneInsegnanteViewModel : BaseViewModel
{
    private readonly IDatabaseService _dbService;

    [ObservableProperty]
    private Insegnanti _insegnante = new();

    [ObservableProperty]
    private string _nome = string.Empty;

    [ObservableProperty]
    private string _cognome = string.Empty;

    [ObservableProperty]
    private string _telefono = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private bool _isEdizione = false;

    public GestioneInsegnanteViewModel(INavigationService navigationService, IDatabaseService dbService)
        : base(navigationService)
    {
        _dbService = dbService;
        Title = "Gestione Insegnante";
    }

    partial void OnInsegnanteChanged(Insegnanti value)
    {
        if (value == null) return;

        Nome = value.Nome ?? string.Empty;
        Cognome = value.Cognome ?? string.Empty;
        Telefono = value.Telefono ?? string.Empty;
        Email = value.Email ?? string.Empty;

        IsEdizione = value.Id > 0;
    }

    [RelayCommand]
    public async Task SalvaInsegnanteAsync()
    {
        if (string.IsNullOrWhiteSpace(Nome) || string.IsNullOrWhiteSpace(Cognome))
        {
            await Shell.Current.DisplayAlert("Attenzione", "Inserisci sia il nome che il cognome.", "OK");
            return;
        }

        await EseguiConCaricamento(async () =>
        {
            Insegnante ??= new Insegnanti();

            Insegnante.Nome = Nome.Trim();
            Insegnante.Cognome = Cognome.Trim();
            Insegnante.Telefono = Telefono?.Trim();
            Insegnante.Email = Email?.Trim();

            await _dbService.SalvaInsegnanteAsync(Insegnante);
            await Shell.Current.Navigation.PopAsync();
        });
    }

    [RelayCommand]
    public async Task EliminaInsegnanteAsync()
    {
        if (Insegnante == null || Insegnante.Id == 0) return;

        bool conferma = await Shell.Current.DisplayAlert(
            "Conferma Eliminazione",
            $"Sei sicuro di voler eliminare l'insegnante '{Insegnante.Nome} {Insegnante.Cognome}'?",
            "Sì, Elimina",
            "Annulla");

        if (conferma)
        {
            await EseguiConCaricamento(async () =>
            {
                await _dbService.EliminaInsegnanteAsync(Insegnante.Id);
                await Shell.Current.Navigation.PopAsync();
            });
        }
    }
}