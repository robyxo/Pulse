using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pulse.Models;
using Pulse.Services;
using Pulse.Utils;
using System.Collections.ObjectModel;

namespace Pulse.ViewModels;

public partial class InsegnantiViewModel : BaseViewModel
{
    private readonly IDatabaseService _dbService;

    [ObservableProperty]
    private ObservableCollection<Insegnanti> _listaInsegnanti = new();

    public InsegnantiViewModel(INavigationService navigationService, IDatabaseService dbService)
        : base(navigationService)
    {
        _dbService = dbService;
        Title = "Gestione Insegnanti";
    }

    [RelayCommand]
    public async Task CaricaInsegnantiAsync()
    {
        await EseguiConCaricamento(async () =>
        {
            ListaInsegnanti.Clear();
            var insegnanti = await _dbService.GetInsegnantiAttiviAsync();
            foreach (var item in insegnanti)
            {
                ListaInsegnanti.Add(item);
            }
        });
    }

    [RelayCommand]
    public async Task NuovoInsegnanteAsync()
    {
        var parametri = new Dictionary<string, object>
    {
        { "Insegnante", new Insegnanti { Attivo = 1 } }
    };
        await Shell.Current.GoToAsync(AppRoutes.Insegnanti.GestioneInsegnante, parametri);
    }

    [RelayCommand]
    public async Task ModificaInsegnanteAsync(Insegnanti insegnante)
    {
        if (insegnante == null) return;

        var parametri = new Dictionary<string, object>
        {
            { "Insegnante", insegnante }
        };
        await Shell.Current.GoToAsync(AppRoutes.Insegnanti.GestioneInsegnante, parametri);
    }

    [RelayCommand]
    public async Task EliminaInsegnanteAsync(Insegnanti insegnante)
    {
        if (insegnante == null) return;

        bool confermato = await Shell.Current.DisplayAlert(
            "Conferma Eliminazione",
            $"Sei sicuro di voler eliminare l'insegnante '{insegnante.Nome} {insegnante.Cognome}'?",
            "Sì, Elimina",
            "Annulla");

        if (confermato)
        {
            await EseguiConCaricamento(async () =>
            {
                await _dbService.EliminaInsegnanteAsync(insegnante.Id);
                await CaricaInsegnantiAsync();
            });
        }
    }
}