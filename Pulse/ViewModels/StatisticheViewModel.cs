using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pulse.DTO;
using Pulse.Models;
using Pulse.Services;
using Pulse.Utils;
using Pulse.Views.Popups;

namespace Pulse.ViewModels;

public partial class StatisticheViewModel : BaseViewModel
{
    private readonly IStatisticheService _statisticheService;
    private readonly IImpostazioniService _impostazioniService;

    // Chi puo' creare, modificare ed eliminare le query salvate.
    // Oggi tutti: le query girano su una connessione in SOLA LETTURA, quindi
    // non possono rovinare i dati. Per riservarle a te, metti:
    //     private static bool PuoGestireQuery => ModalitaSviluppo;
    private static bool PuoGestireQuery => true;

    // Le query segnate "solo amministratore" si vedono solo quando l'app
    // gira da Visual Studio (build Debug), non nelle installazioni delle scuole.
#if DEBUG
    private const bool ModalitaSviluppo = true;
#else
    private const bool ModalitaSviluppo = false;
#endif

    private const int IndicePersonalizzato = 5;

    private bool _impostandoPeriodo;
    private bool _caricamentoIniziale = true;
    private RisultatoQueryDTO? _ultimoRisultato;

    // ================================================
    // PERIODO
    // ================================================

    public List<string> OpzioniPeriodo { get; } = new()
    {
        "Anno scolastico in corso",
        "Anno scolastico precedente",
        "Mese corrente",
        "Mese precedente",
        "Anno solare",
        "Personalizzato"
    };

    [ObservableProperty]
    private int _periodoSelezionatoIndex;

    [ObservableProperty]
    private DateTime _dataDal;

    [ObservableProperty]
    private DateTime _dataAl;

    [ObservableProperty]
    private string _periodoTesto = string.Empty;

    // ================================================
    // DATI
    // ================================================

    [ObservableProperty]
    private RiepilogoStatisticheDTO _riepilogo = new();

    [ObservableProperty]
    private ObservableCollection<VoceGraficoDTO> _entratePerMese = new();

    [ObservableProperty]
    private ObservableCollection<VoceGraficoDTO> _corsi = new();

    [ObservableProperty]
    private ObservableCollection<VoceGraficoDTO> _tipiAbbonamento = new();

    [ObservableProperty]
    private ObservableCollection<VoceInsegnanteDTO> _insegnanti = new();

    [ObservableProperty]
    private ObservableCollection<VoceGraficoDTO> _lezioniPiuFrequentate = new();

    [ObservableProperty]
    private ObservableCollection<VoceGraficoDTO> _lezioniMenoFrequentate = new();

    [ObservableProperty]
    private bool _mostraLezioni;

    [ObservableProperty]
    private bool _mostraLezioniMeno;

    [ObservableProperty]
    private ObservableCollection<GruppoCampoExtraDTO> _campiExtra = new();

    [ObservableProperty]
    private bool _nessunCorso;

    [ObservableProperty]
    private bool _mostraInsegnanti;

    [ObservableProperty]
    private bool _mostraCampiExtra;

    [ObservableProperty]
    private bool _saldoNegativo;

    // ================================================
    // QUERY SALVATE
    // ================================================

    [ObservableProperty]
    private ObservableCollection<QuerySalvate> _listaQuery = new();

    [ObservableProperty]
    private bool _nessunaQuery = true;

    [ObservableProperty]
    private bool _mostraRisultato;

    [ObservableProperty]
    private string _titoloRisultato = string.Empty;

    [ObservableProperty]
    private string _infoRisultato = string.Empty;

    [ObservableProperty]
    private string _testoRisultato = string.Empty;

    [ObservableProperty]
    private bool _risultatoInErrore;

    public bool PuoModificareQuery => PuoGestireQuery;

    public StatisticheViewModel(IStatisticheService statisticheService, IImpostazioniService impostazioniService)
    {
        _statisticheService = statisticheService;
        _impostazioniService = impostazioniService;
        Title = "Statistiche";

        ImpostaPeriodo(0);
    }

    // ================================================
    // CARICAMENTO
    // ================================================

    /// <summary>
    /// Primo caricamento della pagina. Le volte successive (per esempio al
    /// ritorno dall'editor delle query) non ricalcola tutto: c'e' Aggiorna.
    /// </summary>
    public async Task CaricaAsync()
    {
        if (!_caricamentoIniziale) return;

        await EseguiConCaricamento(async () =>
        {
            await CaricaStatisticheInternoAsync();
            await CaricaQueryInternoAsync();
        });
        _caricamentoIniziale = false;
    }

    [RelayCommand]
    public Task AggiornaAsync() => EseguiConCaricamento(CaricaStatisticheInternoAsync);

    private async Task CaricaStatisticheInternoAsync()
    {
        var impostazioni = await _impostazioniService.GetImpostazioniAsync();
        bool funzioneMaestro = impostazioni.FunzioneMaestroAvanzataAttiva == 1;

        var dati = await _statisticheService.GetRiepilogoAsync(DataDal, DataAl);

        Riepilogo = dati;
        SaldoNegativo = dati.Saldo < 0;
        EntratePerMese = new ObservableCollection<VoceGraficoDTO>(dati.EntratePerMese);
        Corsi = new ObservableCollection<VoceGraficoDTO>(dati.Corsi);
        TipiAbbonamento = new ObservableCollection<VoceGraficoDTO>(dati.TipiAbbonamento);
        LezioniPiuFrequentate = new ObservableCollection<VoceGraficoDTO>(dati.LezioniPiuFrequentate);
        LezioniMenoFrequentate = new ObservableCollection<VoceGraficoDTO>(dati.LezioniMenoFrequentate);
        MostraLezioni = dati.LezioniPiuFrequentate.Count > 0;
        MostraLezioniMeno = dati.LezioniMenoFrequentate.Count > 0;
        Insegnanti = new ObservableCollection<VoceInsegnanteDTO>(dati.Insegnanti);
        CampiExtra = new ObservableCollection<GruppoCampoExtraDTO>(dati.CampiExtra);

        NessunCorso = dati.Corsi.Count == 0;
        // Ore e tariffe hanno senso solo con la gestione maestri attiva; i
        // pagamenti gia' registrati si mostrano comunque.
        MostraInsegnanti = dati.Insegnanti.Count > 0 && (funzioneMaestro || dati.Uscite > 0);
        MostraCampiExtra = dati.CampiExtra.Count > 0;
    }

    // ================================================
    // PERIODO
    // ================================================

    partial void OnPeriodoSelezionatoIndexChanged(int value)
    {
        if (_impostandoPeriodo || value == IndicePersonalizzato) return;

        ImpostaPeriodo(value);
        if (!_caricamentoIniziale) _ = AggiornaAsync();
    }

    partial void OnDataDalChanged(DateTime value) => DataModificataAMano();

    partial void OnDataAlChanged(DateTime value) => DataModificataAMano();

    private void DataModificataAMano()
    {
        AggiornaPeriodoTesto();
        if (_impostandoPeriodo) return;

        // Toccare una data a mano porta il periodo su "Personalizzato",
        // senza ricaricare a ogni clic: si preme Aggiorna.
        _impostandoPeriodo = true;
        PeriodoSelezionatoIndex = IndicePersonalizzato;
        _impostandoPeriodo = false;
    }

    private void ImpostaPeriodo(int indice)
    {
        var oggi = DateTime.Today;
        var inizioMese = new DateTime(oggi.Year, oggi.Month, 1);

        (DateTime dal, DateTime al) = indice switch
        {
            1 => _statisticheService.AnnoScolastico(oggi.AddYears(-1)),
            2 => (inizioMese, inizioMese.AddMonths(1).AddDays(-1)),
            3 => (inizioMese.AddMonths(-1), inizioMese.AddDays(-1)),
            4 => (new DateTime(oggi.Year, 1, 1), new DateTime(oggi.Year, 12, 31)),
            _ => _statisticheService.AnnoScolastico(oggi)
        };

        _impostandoPeriodo = true;
        DataDal = dal;
        DataAl = al;
        PeriodoSelezionatoIndex = indice;
        _impostandoPeriodo = false;

        AggiornaPeriodoTesto();
    }

    private void AggiornaPeriodoTesto() =>
        PeriodoTesto = $"Dal {DataDal:dd/MM/yyyy} al {DataAl:dd/MM/yyyy}";

    // ================================================
    // QUERY SALVATE
    // ================================================

    private async Task CaricaQueryInternoAsync()
    {
        var query = await _statisticheService.GetQuerySalvateAsync(ModalitaSviluppo);
        ListaQuery = new ObservableCollection<QuerySalvate>(query);
        NessunaQuery = query.Count == 0;
    }

    [RelayCommand]
    public async Task NuovaQueryAsync()
    {
        if (!PuoGestireQuery) return;
        await ApriEditorQueryAsync(new QuerySalvate { Nome = string.Empty, TestoQuery = "SELECT " });
    }

    [RelayCommand]
    public async Task ModificaQueryAsync(QuerySalvate query)
    {
        if (query == null || !PuoGestireQuery) return;

        // Si lavora su una copia: se nell'editor si preme Annulla, la riga in
        // elenco resta com'era.
        await ApriEditorQueryAsync(new QuerySalvate
        {
            Id = query.Id,
            Nome = query.Nome,
            Descrizione = query.Descrizione,
            TestoQuery = query.TestoQuery,
            Categoria = query.Categoria,
            SoloAmministratore = query.SoloAmministratore,
            Ordine = query.Ordine,
            UltimaEsecuzione = query.UltimaEsecuzione,
            Attivo = query.Attivo
        });
    }

    private async Task ApriEditorQueryAsync(QuerySalvate query)
    {
        var popup = new QuerySalvataPage(query, ModalitaSviluppo, SalvaQueryDaEditorAsync, ProvaQueryDaEditorAsync);
        await Shell.Current.Navigation.PushModalAsync(popup);
    }

    // EseguiSempre e non EseguiConCaricamento: il salvataggio non deve mai
    // essere saltato perche' nel frattempo la pagina sta caricando qualcosa.
    private Task SalvaQueryDaEditorAsync(QuerySalvate query) =>
        EseguiSempre(async () =>
        {
            await _statisticheService.SalvaQueryAsync(query);
            await CaricaQueryInternoAsync();
        });

    private async Task<(bool Riuscita, string Messaggio)> ProvaQueryDaEditorAsync(string testo)
    {
        var risultato = await _statisticheService.EseguiQueryAsync(testo, maxRighe: 200);
        if (!risultato.Riuscita) return (false, $"❌ {risultato.Errore}");

        // Il risultato compare anche nella pagina Statistiche, sotto l'editor.
        MostraRisultatoQuery("Prova", risultato);

        string righe = risultato.Troncato
            ? "più di 200 righe"
            : risultato.Righe.Count == 1 ? "1 riga" : $"{risultato.Righe.Count} righe";
        return (true, $"✅ La query funziona: {righe}, {risultato.Colonne.Count} colonne.");
    }

    [RelayCommand]
    public async Task EliminaQueryAsync(QuerySalvate query)
    {
        if (query == null || !PuoGestireQuery) return;

        bool conferma = await AlertPopup.ShowConfirmation(
            "Elimina query",
            $"Vuoi eliminare la query '{query.Nome}'?",
            "Sì, Elimina",
            "Annulla");

        if (!conferma) return;

        await EseguiConCaricamento(async () =>
        {
            await _statisticheService.EliminaQueryAsync(query.Id);
            await CaricaQueryInternoAsync();
        });
    }

    [RelayCommand]
    public async Task EseguiQueryAsync(QuerySalvate query)
    {
        if (query == null) return;

        await EseguiConCaricamento(async () =>
        {
            var risultato = await _statisticheService.EseguiQueryAsync(query.TestoQuery, query.Id);
            MostraRisultatoQuery(query.Nome, risultato);
        });
    }

    private void MostraRisultatoQuery(string titolo, RisultatoQueryDTO risultato)
    {
        _ultimoRisultato = risultato.Riuscita ? risultato : null;

        TitoloRisultato = $"🔎 {titolo}";
        RisultatoInErrore = !risultato.Riuscita;

        if (!risultato.Riuscita)
        {
            InfoRisultato = "La query non è andata a buon fine:";
            TestoRisultato = risultato.Errore ?? "Errore sconosciuto.";
        }
        else
        {
            InfoRisultato = risultato.Righe.Count switch
            {
                0 => "Nessuna riga trovata.",
                1 => "1 riga.",
                _ => $"{risultato.Righe.Count} righe."
            };
            if (risultato.Troncato) InfoRisultato += " (Mostrate solo le prime, il risultato era più lungo.)";

            TestoRisultato = ComponiTabellaTesto(risultato);
        }

        MostraRisultato = true;
    }

    [RelayCommand]
    public void ChiudiRisultato()
    {
        MostraRisultato = false;
        _ultimoRisultato = null;
    }

    /// <summary>
    /// Copia il risultato separato da tabulazioni: incollato in Excel finisce
    /// gia' diviso in colonne.
    /// </summary>
    [RelayCommand]
    public async Task CopiaRisultatoAsync()
    {
        if (_ultimoRisultato == null) return;

        var sb = new StringBuilder();
        sb.AppendLine(string.Join('\t', _ultimoRisultato.Colonne));
        foreach (var riga in _ultimoRisultato.Righe)
        {
            sb.AppendLine(string.Join('\t', riga.Select(c => c.Replace('\t', ' ').Replace('\n', ' ').Replace("\r", ""))));
        }

        await Clipboard.Default.SetTextAsync(sb.ToString());
        await AlertPopup.Show("Copiato", "Risultato copiato negli appunti: puoi incollarlo in Excel.");
    }

    // Tabella a larghezza fissa, da mostrare con un carattere monospazio.
    private static string ComponiTabellaTesto(RisultatoQueryDTO risultato)
    {
        const int larghezzaMassima = 40;

        string Taglia(string testo)
        {
            testo = testo.Replace('\n', ' ').Replace("\r", "");
            return testo.Length > larghezzaMassima ? testo[..(larghezzaMassima - 1)] + "…" : testo;
        }

        var intestazioni = risultato.Colonne.Select(Taglia).ToList();
        var righe = risultato.Righe.Select(r => r.Select(Taglia).ToArray()).ToList();

        var larghezze = intestazioni
            .Select((c, i) => Math.Max(c.Length, righe.Count == 0 ? 0 : righe.Max(r => r[i].Length)))
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(" │ ", intestazioni.Select((c, i) => c.PadRight(larghezze[i]))));
        sb.AppendLine(string.Join("─┼─", larghezze.Select(l => new string('─', l))));

        foreach (var riga in righe)
        {
            sb.AppendLine(string.Join(" │ ", riga.Select((c, i) => c.PadRight(larghezze[i]))));
        }

        return sb.ToString().TrimEnd();
    }
}
