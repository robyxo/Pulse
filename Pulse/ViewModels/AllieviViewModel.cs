using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pulse.Models;
using Pulse.Services;
using Pulse.Utils;
using System.Collections.ObjectModel;

namespace Pulse.ViewModels;

public partial class AllieviViewModel : BaseViewModel
{
    private readonly IDatabaseService _dbService;
    private List<Allievi> _listaAllieviCompleta = new();

    [ObservableProperty]
    private ObservableCollection<Allievi> _listaAllievi = new();

    [ObservableProperty]
    private string _testoRicerca = string.Empty;

    public AllieviViewModel(INavigationService navigationService, IDatabaseService dbService)
        : base(navigationService)
    {
        _dbService = dbService;
        Title = "Gestione Allievi";
    }

    partial void OnTestoRicercaChanged(string value)
    {
        ApplicaFiltro();
    }

    [RelayCommand]
    public async Task CaricaAllieviAsync()
    {
        await EseguiConCaricamento(async () =>
        {
            _listaAllieviCompleta = await _dbService.GetAllieviAttiviAsync();
            ApplicaFiltro();
        });
    }

    private void ApplicaFiltro()
    {
        ListaAllievi.Clear();
        var filtrati = string.IsNullOrWhiteSpace(TestoRicerca)
            ? _listaAllieviCompleta
            : _listaAllieviCompleta.Where(a =>
                (a.Nome != null && a.Nome.Contains(TestoRicerca, StringComparison.OrdinalIgnoreCase)) ||
                (a.Cognome != null && a.Cognome.Contains(TestoRicerca, StringComparison.OrdinalIgnoreCase)));

        foreach (var allievo in filtrati)
        {
            ListaAllievi.Add(allievo);
        }
    }

    [RelayCommand]
    public async Task NuovoAllievoAsync()
    {
        var parametri = new Dictionary<string, object>
        {
            { "Allievo", new Allievi { Attivo = 1 } }
        };
        await Shell.Current.GoToAsync(AppRoutes.Allievi.GestioneAllievo, parametri);
    }

    [RelayCommand]
    public async Task ModificaAllievoAsync(Allievi allievo)
    {
        if (allievo == null) return;

        var parametri = new Dictionary<string, object>
        {
            { "Allievo", allievo }
        };
        await Shell.Current.GoToAsync(AppRoutes.Allievi.GestioneAllievo, parametri);
    }

    [RelayCommand]
    public async Task EliminaAllievoAsync(Allievi allievo)
    {
        if (allievo == null) return;

        bool confermato = await Shell.Current.DisplayAlert(
            "Conferma Eliminazione",
            $"Sei sicuro di voler eliminare l'allievo '{allievo.NomeCompleto}'?",
            "Sì, Elimina",
            "Annulla");

        if (confermato)
        {
            await EseguiConCaricamento(async () =>
            {
                await _dbService.EliminaAllievoAsync(allievo.Id);
                await CaricaAllieviAsync();
            });
        }
    }
}