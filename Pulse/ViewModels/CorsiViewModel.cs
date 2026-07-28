// ViewModels/CorsiViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pulse.Models;
using Pulse.Services;
using Pulse.Utils;
using System.Collections.ObjectModel;

namespace Pulse.ViewModels;

public partial class CorsiViewModel : ObservableObject
{
    private readonly IDatabaseService _dbService;

    [ObservableProperty]
    private ObservableCollection<Corsi> _listaCorsi = new();

    [ObservableProperty]
    private bool _isBusy;

    public CorsiViewModel(IDatabaseService dbService)
    {
        _dbService = dbService;
    }

    [RelayCommand]
    public async Task CaricaCorsiAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            ListaCorsi.Clear();
            var corsi = await _dbService.GetCorsiAttiviAsync();
            foreach (var corso in corsi)
            {
                ListaCorsi.Add(corso);
            }
        }
        finally
        {
            IsBusy = false;
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
            await _dbService.EliminaCorsoAsync(corso);
            await CaricaCorsiAsync();
        }
    }
}