using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pulse.Models;
using Pulse.Services;

namespace Pulse.ViewModels;

[QueryProperty(nameof(Allievo), "Allievo")]
public partial class GestioneAllievoViewModel : BaseViewModel
{
    private readonly IDatabaseService _dbService;

    [ObservableProperty]
    private Allievi _allievo = new();

    [ObservableProperty]
    private string _nome = string.Empty;

    [ObservableProperty]
    private string _cognome = string.Empty;

    [ObservableProperty]
    private string _codiceFiscale = string.Empty;

    [ObservableProperty]
    private string _telefono = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private bool _isEdizione = false;

    public GestioneAllievoViewModel(INavigationService navigationService, IDatabaseService dbService)
        : base(navigationService)
    {
        _dbService = dbService;
        Title = "Gestione Allievo";
    }

    partial void OnAllievoChanged(Allievi value)
    {
        if (value == null) return;

        Nome = value.Nome ?? string.Empty;
        Cognome = value.Cognome ?? string.Empty;
        CodiceFiscale = value.CodiceFiscale ?? string.Empty;
        Telefono = value.Telefono ?? string.Empty;
        Email = value.Email ?? string.Empty;

        IsEdizione = value.Id > 0;
    }

    [RelayCommand]
    public async Task SalvaAllievoAsync()
    {
        if (string.IsNullOrWhiteSpace(Nome) || string.IsNullOrWhiteSpace(Cognome))
        {
            await Shell.Current.DisplayAlert("Attenzione", "Inserisci sia il nome che il cognome.", "OK");
            return;
        }

        await EseguiConCaricamento(async () =>
        {
            Allievo ??= new Allievi();

            Allievo.Nome = Nome.Trim();
            Allievo.Cognome = Cognome.Trim();
            Allievo.CodiceFiscale = CodiceFiscale?.Trim().ToUpper();
            Allievo.Telefono = Telefono?.Trim();
            Allievo.Email = Email?.Trim();

            await _dbService.SalvaAllievoAsync(Allievo);
            await Shell.Current.Navigation.PopAsync();
        });
    }

    [RelayCommand]
    public async Task EliminaAllievoAsync()
    {
        if (Allievo == null || Allievo.Id == 0) return;

        bool conferma = await Shell.Current.DisplayAlert(
            "Conferma Eliminazione",
            $"Sei sicuro di voler eliminare l'allievo '{Nome} {Cognome}'?",
            "Sì, Elimina",
            "Annulla");

        if (conferma)
        {
            await EseguiConCaricamento(async () =>
            {
                await _dbService.EliminaAllievoAsync(Allievo.Id);
                await Shell.Current.Navigation.PopAsync();
            });
        }
    }
}