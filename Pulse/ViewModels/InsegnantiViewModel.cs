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
    private List<Insegnanti> _listaInsegnantiCompleta = new();

    [ObservableProperty]
    private ObservableCollection<Insegnanti> _listaInsegnanti = new();

    [ObservableProperty]
    private string _testoRicerca = string.Empty;

    public InsegnantiViewModel(INavigationService navigationService, IDatabaseService dbService)
        : base(navigationService)
    {
        _dbService = dbService;
        Title = "Gestione Insegnanti";
    }

    partial void OnTestoRicercaChanged(string value)
    {
        ApplicaFiltro();
    }

    [RelayCommand]
    public async Task CaricaInsegnantiAsync()
    {
        await EseguiConCaricamento(async () =>
        {
            _listaInsegnantiCompleta = await _dbService.GetInsegnantiAttiviAsync();
            ApplicaFiltro();
        });
    }

    private void ApplicaFiltro()
    {
        ListaInsegnanti.Clear();
        var filtrati = string.IsNullOrWhiteSpace(TestoRicerca)
            ? _listaInsegnantiCompleta
            : _listaInsegnantiCompleta.Where(i =>
                (i.Nome != null && i.Nome.Contains(TestoRicerca, StringComparison.OrdinalIgnoreCase)) ||
                (i.Cognome != null && i.Cognome.Contains(TestoRicerca, StringComparison.OrdinalIgnoreCase)));

        foreach (var item in filtrati)
        {
            ListaInsegnanti.Add(item);
        }
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