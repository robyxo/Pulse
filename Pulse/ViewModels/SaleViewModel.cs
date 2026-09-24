using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pulse.Models;
using Pulse.Services;
using Pulse.Utils;
using System.Collections.ObjectModel;

namespace Pulse.ViewModels;

public partial class SaleViewModel : BaseViewModel
{
    private readonly IDatabaseService _dbService;
    private List<Sale> _listaSaleCompleta = new();

    [ObservableProperty]
    private ObservableCollection<Sale> _listaSale = new();

    [ObservableProperty]
    private string _testoRicerca = string.Empty;

    public SaleViewModel(IDatabaseService dbService)
    {
        _dbService = dbService;
        Title = "Gestione Sale";
    }

    partial void OnTestoRicercaChanged(string value)
    {
        ApplicaFiltro();
    }

    public async Task CaricaSaleAsync()
    {
        await EseguiConCaricamento(CaricaSaleInternoAsync);
    }

    private async Task CaricaSaleInternoAsync()
    {
        _listaSaleCompleta = await _dbService.GetSaleAttiveAsync();
        ApplicaFiltro();
    }

    private void ApplicaFiltro()
    {
        ListaSale.Clear();

        var filtrate = string.IsNullOrWhiteSpace(TestoRicerca)
            ? _listaSaleCompleta
            : _listaSaleCompleta.Where(s => s.Nome != null && s.Nome.Contains(TestoRicerca, StringComparison.OrdinalIgnoreCase));

        foreach (var sala in filtrate)
        {
            ListaSale.Add(sala);
        }
    }

    [RelayCommand]
    public async Task NuovaSalaAsync()
    {
        var parametri = new Dictionary<string, object>
        {
            { "Sala", new Sale { Colore = "#0EA5E9", Attivo = 1 } }
        };
        await Shell.Current.GoToAsync(AppRoutes.Sale.GestioneSala, parametri);
    }

    [RelayCommand]
    public async Task ModificaSalaAsync(Sale sala)
    {
        if (sala == null) return;

        var parametri = new Dictionary<string, object>
        {
            { "Sala", sala }
        };
        await Shell.Current.GoToAsync(AppRoutes.Sale.GestioneSala, parametri);
    }

    [RelayCommand]
    public async Task EliminaSalaAsync(Sale sala)
    {
        if (sala == null) return;

        bool confermato = await AlertPopup.ShowConfirmation(
            "Conferma Eliminazione",
            $"Sei sicuro di voler eliminare la sala '{sala.Nome}'?\n\nLe lezioni che la usano restano al loro posto, ma senza sala assegnata.",
            "Sì, Elimina",
            "Annulla");

        if (confermato)
        {
            await EseguiConCaricamento(async () =>
            {
                await _dbService.EliminaSalaAsync(sala.Id);
                await CaricaSaleInternoAsync();
            });
        }
    }
}
