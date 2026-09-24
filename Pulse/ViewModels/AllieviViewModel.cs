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
        "Da Abbonare",
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

        // Fuori dal caricamento: lo spinner è già spento mentre la segreteria decide.
        await AvvisaAllieviDaAbbonareAsync();
    }

    /// <summary>
    /// Promemoria all'apertura della pagina: chi si è registrato (per esempio dal
    /// tablet) e non ha ancora un abbonamento. Copre il caso in cui il popup della
    /// registrazione sia stato chiuso, o in quel momento non ci fosse nessuno al PC.
    /// </summary>
    private async Task AvvisaAllieviDaAbbonareAsync()
    {
        var daAbbonare = _listaCompletaDTO
            .Where(x => x.IsDaAbbonare)
            .OrderBy(x => x.Allievo.DataRegistrazione)
            .ToList();

        if (daAbbonare.Count == 0) return;

        if (daAbbonare.Count == 1)
        {
            var allievo = daAbbonare[0];

            bool apri = await AlertPopup.ShowConfirmation(
                "⚠️ Allievo da abbonare",
                $"{allievo.NomeCompleto} si è registrato ma non ha ancora un abbonamento.\n\nVuoi aprire la scheda per farlo adesso?",
                "Apri scheda",
                "Più tardi");

            if (apri) await ModificaAllievoAsync(allievo);
            return;
        }

        // Più allievi: si sceglie chi aprire. Nome e data di registrazione, per gli omonimi.
        var voci = daAbbonare.Select(x => x.NomeConRegistrazione).ToArray();

        string scelta = await AlertPopup.ShowActionSheet(
            $"⚠️ {daAbbonare.Count} allievi registrati senza abbonamento",
            "Più tardi",
            null,
            voci);

        int indice = Array.IndexOf(voci, scelta);
        if (indice >= 0) await ModificaAllievoAsync(daAbbonare[indice]);
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

        // Per ogni allievo si tiene l'ultimo abbonamento di OGNI corso, non uno solo:
        // altrimenti chi frequenta piu' corsi mostrerebbe lo stato di un corso a caso
        // e un abbonamento in scadenza resterebbe nascosto dietro uno ancora valido.
        var abbonamentiPerAllievo = abbonamenti
            .GroupBy(a => a.AllievoId)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(a => a.CorsoId)
                      .Select(perCorso => perCorso
                          .OrderByDescending(a => a.DataScadenza)
                          .ThenByDescending(a => a.Id)
                          .First())
                      .ToList());

        _listaCompletaDTO = allievi
            .Select(a => new AllievoTabellaDTO
            {
                Allievo = a,
                AbbonamentiPerCorso = abbonamentiPerAllievo.TryGetValue(a.Id, out var lista)
                    ? lista
                    : new List<Abbonamenti>()
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

        // 2. Filtro per Corso — vale su QUALSIASI corso dell'allievo, non solo
        //    sull'ultimo: altrimenti filtrando "Salsa" sparirebbe chi fa Salsa
        //    ma ha un altro abbonamento con scadenza piu' lontana.
        if (CorsoSelezionatoFiltro != null && CorsoSelezionatoFiltro.Id > 0)
        {
            int corsoId = CorsoSelezionatoFiltro.Id;
            filtrati = filtrati.Where(x => x.FrequentaCorso(corsoId));
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

        bool confermato = await AlertPopup.ShowConfirmation(
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