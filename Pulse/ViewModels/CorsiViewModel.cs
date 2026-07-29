using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pulse.Models;
using Pulse.Services;
using Pulse.Utils;
using System.Collections.ObjectModel;

namespace Pulse.ViewModels;

public partial class CorsiViewModel : BaseViewModel
{
    private readonly IDatabaseService _dbService;
    private List<Corsi> _listaCorsiCompleta = new();

    [ObservableProperty]
    private ObservableCollection<Corsi> _listaCorsi = new();

    [ObservableProperty]
    private string _testoRicerca = string.Empty;

    public CorsiViewModel(INavigationService navigationService, IDatabaseService dbService)
        : base(navigationService)
    {
        _dbService = dbService;
        Title = "Gestione Corsi";
    }

    partial void OnTestoRicercaChanged(string value)
    {
        ApplicaFiltro();
    }

    [RelayCommand]
    public async Task CaricaCorsiAsync()
    {
        await EseguiConCaricamento(async () =>
        {
            _listaCorsiCompleta = await _dbService.GetCorsiAttiviAsync();
            ApplicaFiltro();
        });
    }

    private void ApplicaFiltro()
    {
        ListaCorsi.Clear();
        var filtrati = string.IsNullOrWhiteSpace(TestoRicerca)
            ? _listaCorsiCompleta
            : _listaCorsiCompleta.Where(c => c.Nome != null && c.Nome.Contains(TestoRicerca, StringComparison.OrdinalIgnoreCase));

        foreach (var corso in filtrati)
        {
            ListaCorsi.Add(corso);
        }
    }

    [RelayCommand]
    public async Task NuovoCorsoAsync()
    {
        var parametri = new Dictionary<string, object>
        {
            { "Corso", new Corsi { Colore = "#4F46E5", Attivo = 1 } }
        };
        await Shell.Current.GoToAsync(AppRoutes.Corsi.GestioneCorso, parametri);
    }

    [RelayCommand]
    public async Task ModificaCorsoAsync(Corsi corso)
    {
        if (corso == null) return;

        var parametri = new Dictionary<string, object>
        {
            { "Corso", corso }
        };
        await Shell.Current.GoToAsync(AppRoutes.Corsi.GestioneCorso, parametri);
    }

    [RelayCommand]
    public async Task EliminaCorsoAsync(Corsi corso)
    {
        if (corso == null) return;

        bool confermato = await Shell.Current.DisplayAlert(
            "Conferma Eliminazione",
            $"Sei sicuro di voler eliminare il corso '{corso.Nome}'?",
            "Sì, Elimina",
            "Annulla");

        if (confermato)
        {
            await EseguiConCaricamento(async () =>
            {
                await _dbService.EliminaCorsoAsync(corso);
                await CaricaCorsiAsync();
            });
        }
    }
}