using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pulse.DTO;
using Pulse.Models;
using Pulse.Services;
using Pulse.Utils;
using System.Collections.ObjectModel;

namespace Pulse.ViewModels;

public partial class AllieviViewModel : BaseViewModel
{
    private readonly IDatabaseService _dbService;
    private List<AllievoTabellaDTO> _listaCompletaDTO = new();

    [ObservableProperty]
    private ObservableCollection<AllievoTabellaDTO> _listaAllievi = new();

    // 🎓 1. Filtro Corsi
    [ObservableProperty]
    private ObservableCollection<Corsi> _listaFiltroCorsi = new();

    [ObservableProperty]
    private Corsi? _corsoSelezionatoFiltro;

    // 🏷️ 2. Filtro Stato Abbonamento
    [ObservableProperty]
    private string _statoSelezionatoFiltro = "Tutti gli Stati";

    // 🔍 3. Ricerca Testuale
    [ObservableProperty]
    private string _testoRicerca = string.Empty;

    public List<string> StatiDisponibili { get; } = new()
    {
        "Tutti gli Stati",
        "Attivo",
        "In Scadenza",
        "In Pausa",
        "Scaduto",
        "Nessuno"
    };

    public AllieviViewModel(IDatabaseService dbService)
    {
        _dbService = dbService;
        Title = "Gestione Allievi";
    }

    partial void OnTestoRicercaChanged(string value) => ApplicaFiltri();
    partial void OnCorsoSelezionatoFiltroChanged(Corsi? value) => ApplicaFiltri();
    partial void OnStatoSelezionatoFiltroChanged(string value) => ApplicaFiltri();

    public async Task CaricaAllieviAsync()
    {
        await EseguiConCaricamento(CaricaAllieviInternoAsync);
    }

    private async Task CaricaAllieviInternoAsync()
    {
        // 1. Carica Corsi per il filtro
        var corsi = await _dbService.GetCorsiAttiviAsync();
        var corsiFiltro = new List<Corsi> { new Corsi { Id = 0, Nome = "Tutti i Corsi" } };
        corsiFiltro.AddRange(corsi);
        ListaFiltroCorsi = new ObservableCollection<Corsi>(corsiFiltro);
        CorsoSelezionatoFiltro = ListaFiltroCorsi.FirstOrDefault(c => c.Id == 0);

        // 2. Seleziona lo stato di default
        StatoSelezionatoFiltro = "Tutti gli Stati";

        // 3. Allievi e abbonamenti in due sole query, poi si incrociano in memoria
        var allievi = await _dbService.GetAllieviAttiviAsync();
        var abbonamenti = await _dbService.GetAbbonamentiAttiviAsync();

        var ultimoPerAllievo = abbonamenti
            .GroupBy(a => a.AllievoId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.DataScadenza).First());

        _listaCompletaDTO = allievi
            .Select(a => new AllievoTabellaDTO
            {
                Allievo = a,
                UltimoAbbonamento = ultimoPerAllievo.TryGetValue(a.Id, out var abb) ? abb : null
            })
            .ToList();

        ApplicaFiltri();
    }

    private void ApplicaFiltri()
    {
        var filtrati = _listaCompletaDTO.AsEnumerable();

        // 1. Filtro Ricerca
        if (!string.IsNullOrWhiteSpace(TestoRicerca))
        {
            filtrati = filtrati.Where(x =>
                x.NomeCompleto.Contains(TestoRicerca, StringComparison.OrdinalIgnoreCase) ||
                (x.Allievo.CodiceFiscale != null && x.Allievo.CodiceFiscale.Contains(TestoRicerca, StringComparison.OrdinalIgnoreCase)));
        }

        // 2. Filtro per Corso
        if (CorsoSelezionatoFiltro != null && CorsoSelezionatoFiltro.Id > 0)
        {
            filtrati = filtrati.Where(x => x.UltimoAbbonamento != null && x.UltimoAbbonamento.CorsoId == CorsoSelezionatoFiltro.Id);
        }

        // 3. Filtro per Stato Abbonamento
        if (!string.IsNullOrWhiteSpace(StatoSelezionatoFiltro) && StatoSelezionatoFiltro != "Tutti gli Stati")
        {
            filtrati = filtrati.Where(x => x.StatoChiave == StatoSelezionatoFiltro);
        }

        ListaAllievi = new ObservableCollection<AllievoTabellaDTO>(filtrati.ToList());
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
    public async Task ModificaAllievoAsync(AllievoTabellaDTO item)
    {
        if (item?.Allievo == null) return;

        var parametri = new Dictionary<string, object>
        {
            { "Allievo", item.Allievo }
        };
        await Shell.Current.GoToAsync(AppRoutes.Allievi.GestioneAllievo, parametri);
    }

    [RelayCommand]
    public async Task EliminaAllievoAsync(AllievoTabellaDTO item)
    {
        if (item?.Allievo == null) return;

        bool confermato = await Shell.Current.DisplayAlert(
            "Conferma Eliminazione",
            $"Sei sicuro di voler eliminare l'allievo '{item.NomeCompleto}'?",
            "Sì, Elimina",
            "Annulla");

        if (confermato)
        {
            await EseguiConCaricamento(async () =>
            {
                await _dbService.EliminaAllievoAsync(item.Allievo.Id);
                await CaricaAllieviInternoAsync();
            });
        }
    }
}