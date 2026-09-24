using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pulse.Models;
using Pulse.Services;
using System.Collections.ObjectModel;
using Pulse.Views.Popups;
using Pulse.Utils;

namespace Pulse.ViewModels;

[QueryProperty(nameof(Allievo), "Allievo")]
public partial class GestioneAllievoViewModel : BaseViewModel
{
    private readonly IDatabaseService _dbService;
    private readonly IImpostazioniService _impostazioniService;
    private readonly RicevutaService _ricevutaService;
    private readonly PrivacyDocumentService _privacyDocumentService;
    private List<Abbonamenti> _listaAbbonamentiMaster = new();

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

    // --- NUOVI CAMPI ANAGRAFICA (allineati al Model) ---
    [ObservableProperty]
    private string _sesso = "M";

    [ObservableProperty]
    private DateTime _dataNascita = DateTime.Today.AddYears(-20);

    [ObservableProperty]
    private string _indirizzo = string.Empty;

    [ObservableProperty]
    private string _nCivico = string.Empty;

    [ObservableProperty]
    private string _cap = string.Empty;

    [ObservableProperty]
    private string _citta = string.Empty;

    [ObservableProperty]
    private string _provincia = string.Empty;

    [ObservableProperty]
    private string _cellulare = string.Empty;

    [ObservableProperty]
    private string _allegati = string.Empty;

    public List<string> OpzioniSesso { get; } = new() { "M", "F", "Altro" };

    [ObservableProperty]
    private bool _isEdizione = false;

    [ObservableProperty]
    private bool _mostraBottonePrivacy = true;

    // --- STATO DEL MODULO PRIVACY ---
    // La firma resta sulla carta: qui si registra solo che è stata apposta,
    // ed è quel momento a far nascere il PDF da conservare.
    private Privacy? _privacyCorrente;

    [ObservableProperty]
    private bool _mostraStatoPrivacy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostraBottoneFirma))]
    private bool _privacyFirmata;

    /// <summary>Il bottone per firmare si vede solo finché la firma non è registrata.</summary>
    public bool MostraBottoneFirma => !PrivacyFirmata;

    [ObservableProperty]
    private string _statoPrivacyTesto = string.Empty;

    [ObservableProperty]
    private string _statoPrivacyColore = "#94A3B8";

    [ObservableProperty]
    private bool _haPdfArchiviato;

    // --- TABELLA E PAGINAZIONE ABBONAMENTI ---
    [ObservableProperty]
    private ObservableCollection<Abbonamenti> _listaAbbonamentiPaginata = new();

    [ObservableProperty]
    private ObservableCollection<string> _anniDisponibili = new();

    [ObservableProperty]
    private string _annoSelezionato = "Tutti gli anni";

    [ObservableProperty]
    private string _testoRicercaAbbonamento = string.Empty;

    [ObservableProperty]
    private int _paginaCorrente = 1;

    [ObservableProperty]
    private int _totalePagine = 1;

    [ObservableProperty]
    private string _testoPaginazione = "Pagina 1 di 1";

    [ObservableProperty]
    private bool _haPaginaPrecedente = false;

    [ObservableProperty]
    private bool _haPaginaSuccessiva = false;

    private const int ElementiPerPagina = 5;

    private bool _stampaRicevutaCortesiaAttiva = true;

    public GestioneAllievoViewModel(IDatabaseService dbService, IImpostazioniService impostazioniService, RicevutaService ricevutaService, PrivacyDocumentService privacyDocumentService)
    {
        _dbService = dbService;
        _impostazioniService = impostazioniService;
        _ricevutaService = ricevutaService;
        _privacyDocumentService = privacyDocumentService;
        Title = "Gestione Allievo";

        _ = CaricaFlagImpostazioniAsync();
    }

    private async Task CaricaFlagImpostazioniAsync()
    {
        var impostazioni = await _impostazioniService.GetImpostazioniAsync();
        _stampaRicevutaCortesiaAttiva = impostazioni.StampaRicevutaCortesia == 1;
        MostraBottonePrivacy = impostazioni.StampaDocumentoPrivacy == 1
                               && PrivacyDocumentService.AlmenoUnModelloDisponibile;
    }

    partial void OnAllievoChanged(Allievi value)
    {
        if (value == null) return;

        Nome = value.Nome ?? string.Empty;
        Cognome = value.Cognome ?? string.Empty;
        CodiceFiscale = value.CodiceFiscale ?? string.Empty;
        Telefono = value.Telefono ?? string.Empty;
        Email = value.Email ?? string.Empty;

        Sesso = string.IsNullOrWhiteSpace(value.Sesso) ? "M" : value.Sesso;
        DataNascita = DateTime.TryParse(value.DataNascita, out var dataNascitaParsata)
            ? dataNascitaParsata
            : DateTime.Today.AddYears(-20);
        Indirizzo = value.Indirizzo ?? string.Empty;
        NCivico = value.NCivico ?? string.Empty;
        Cap = value.Cap ?? string.Empty;
        Citta = value.Citta ?? string.Empty;
        Provincia = value.Provincia ?? string.Empty;
        Cellulare = value.Cellulare ?? string.Empty;
        Allegati = value.Allegati ?? string.Empty;

        IsEdizione = value.Id > 0;

        _ = CaricaAbbonamentiAsync();
        _ = CaricaStatoPrivacyAsync();
    }

    private async Task CaricaStatoPrivacyAsync()
    {
        // Ha senso solo su un allievo già a database: prima non c'è nulla da firmare.
        MostraStatoPrivacy = Allievo.Id > 0 && MostraBottonePrivacy;

        if (!MostraStatoPrivacy)
        {
            _privacyCorrente = null;
            PrivacyFirmata = false;
            HaPdfArchiviato = false;
            return;
        }

        var righe = await _dbService.GetPrivacyAllievoAsync(Allievo.Id);
        _privacyCorrente = righe.OrderByDescending(p => p.Id).FirstOrDefault();

        PrivacyFirmata = _privacyCorrente?.Firmato == 1;

        string? pdf = _privacyCorrente?.PercorsoPdf;
        HaPdfArchiviato = !string.IsNullOrWhiteSpace(pdf) && File.Exists(pdf);

        AggiornaTestoPrivacy();
    }

    private void AggiornaTestoPrivacy()
    {
        if (PrivacyFirmata)
        {
            string data = _privacyCorrente?.DataFirma?.ToString("dd/MM/yyyy") ?? "data non registrata";
            StatoPrivacyTesto = HaPdfArchiviato
                ? $"Firmato il {data} · copia PDF archiviata"
                : $"Firmato il {data} · PDF non archiviato";
            StatoPrivacyColore = HaPdfArchiviato ? "#10B981" : "#F59E0B";
        }
        else
        {
            StatoPrivacyTesto = "Modulo privacy non ancora firmato";
            StatoPrivacyColore = "#94A3B8";
        }
    }

    partial void OnTestoRicercaAbbonamentoChanged(string value)
    {
        PaginaCorrente = 1;
        ApplicaFiltroEPaginazione();
    }

    partial void OnAnnoSelezionatoChanged(string value)
    {
        PaginaCorrente = 1;
        ApplicaFiltroEPaginazione();
    }

    private async Task CaricaAbbonamentiAsync()
    {
        if (Allievo.Id > 0)
        {
            var abbonamenti = await _dbService.GetAbbonamentiAllievoAsync(Allievo.Id);
            _listaAbbonamentiMaster = abbonamenti.OrderByDescending(a => a.DataInizio).ToList();

            AggiornaAnniDisponibili();

            PaginaCorrente = 1;
            ApplicaFiltroEPaginazione();
        }
    }

    /// <summary>
    /// Ricostruisce l'elenco degli anni del filtro a partire dagli abbonamenti in memoria.
    /// Va richiamato ogni volta che la lista cambia (nuovo abbonamento, rinnovo, eliminazione),
    /// altrimenti un abbonamento di un anno non ancora presente resterebbe invisibile nel filtro.
    /// </summary>
    private void AggiornaAnniDisponibili()
    {
        string selezionePrecedente = AnnoSelezionato;

        var anni = _listaAbbonamentiMaster
            .Select(a => a.DataInizio.Year.ToString())
            .Distinct()
            .OrderByDescending(y => y)
            .ToList();

        anni.Insert(0, "Tutti gli anni");
        AnniDisponibili = new ObservableCollection<string>(anni);

        // Mantiene la selezione se l'anno esiste ancora, altrimenti torna a "Tutti gli anni"
        AnnoSelezionato = anni.Contains(selezionePrecedente)
            ? selezionePrecedente
            : anni[0];
    }

    private void ApplicaFiltroEPaginazione()
    {
        var filtrati = _listaAbbonamentiMaster.AsEnumerable();

        // 1. Filtro Anno
        if (!string.IsNullOrWhiteSpace(AnnoSelezionato) && AnnoSelezionato != "Tutti gli anni" && int.TryParse(AnnoSelezionato, out int annoInt))
        {
            filtrati = filtrati.Where(a => a.DataInizio.Year == annoInt);
        }

        // 2. Filtro Testo (per nome corso o tipo abbonamento)
        if (!string.IsNullOrWhiteSpace(TestoRicercaAbbonamento))
        {
            filtrati = filtrati.Where(a =>
                (a.Corso != null && a.Corso.Nome.Contains(TestoRicercaAbbonamento, StringComparison.OrdinalIgnoreCase)) ||
                a.TipoAbbonamento.Contains(TestoRicercaAbbonamento, StringComparison.OrdinalIgnoreCase));
        }

        // 3. In tabella compare SOLO l'ultimo abbonamento di ogni corso.
        //    Lo storico completo di quel corso si apre con l'icona 📋 sulla riga.
        var listaFiltrata = filtrati
            .GroupBy(a => a.CorsoId)
            .Select(g => g
                .OrderByDescending(a => a.DataInizio)
                // A parita' di data vince l'inserimento piu' recente:
                // un abbonamento non ancora salvato (Id 0) e' sempre l'ultimo aggiunto.
                .ThenByDescending(a => a.Id == 0 ? int.MaxValue : a.Id)
                .First())
            .OrderByDescending(a => a.DataInizio)
            .ToList();

        // Calcolo Pagine
        int totaleElementi = listaFiltrata.Count;
        TotalePagine = (int)Math.Ceiling((double)totaleElementi / ElementiPerPagina);
        if (TotalePagine <= 0) TotalePagine = 1;

        if (PaginaCorrente > TotalePagine) PaginaCorrente = TotalePagine;
        if (PaginaCorrente < 1) PaginaCorrente = 1;

        HaPaginaPrecedente = PaginaCorrente > 1;
        HaPaginaSuccessiva = PaginaCorrente < TotalePagine;
        TestoPaginazione = $"Pagina {PaginaCorrente} di {TotalePagine}";

        // Paginazione a 5 record
        var elementiPagina = listaFiltrata
            .Skip((PaginaCorrente - 1) * ElementiPerPagina)
            .Take(ElementiPerPagina)
            .ToList();

        ListaAbbonamentiPaginata = new ObservableCollection<Abbonamenti>(elementiPagina);
    }

    [RelayCommand]
    public void PaginaPrecedente()
    {
        if (HaPaginaPrecedente)
        {
            PaginaCorrente--;
            ApplicaFiltroEPaginazione();
        }
    }

    [RelayCommand]
    public void PaginaSuccessiva()
    {
        if (HaPaginaSuccessiva)
        {
            PaginaCorrente++;
            ApplicaFiltroEPaginazione();
        }
    }

    // 🟢 NUOVO ABBONAMENTO
    [RelayCommand]
    public async Task ApriPopupNuovoAbbonamentoAsync()
    {
        var corsiDisponibili = await _dbService.GetCorsiAttiviAsync();

        if (corsiDisponibili == null || corsiDisponibili.Count == 0)
        {
            await AlertPopup.Show("Attenzione", "Prima devi creare un corso!", "OK");
            return;
        }

        var opzioniCorsi = corsiDisponibili.Select(c => c.Nome).ToArray();
        string corsoSelezionatoNome = await AlertPopup.ShowActionSheet("Seleziona il Corso:", "Annulla", null, opzioniCorsi);

        if (string.IsNullOrEmpty(corsoSelezionatoNome) || corsoSelezionatoNome == "Annulla") return;

        var corsoScelto = corsiDisponibili.FirstOrDefault(c => c.Nome == corsoSelezionatoNome);
        if (corsoScelto == null) return;

        var popup = new NuovoAbbonamentoPage(corsoScelto, CompletaCreazioneAbbonamentoAsync);
        await Shell.Current.Navigation.PushModalAsync(popup);
    }

    private async Task CompletaCreazioneAbbonamentoAsync(Abbonamenti nuovo)
    {
        nuovo.AllievoId = Allievo.Id;
        nuovo.Allievo = Allievo;

        // Se l'allievo è già a database, l'abbonamento si salva subito.
        // Se invece è una scheda nuova (Id ancora 0), l'abbonamento resta in
        // elenco e viene salvato insieme all'anagrafica quando si preme Salva.
        if (Allievo.Id > 0)
        {
            await _dbService.SalvaAbbonamentoAsync(nuovo);
        }

        // In entrambi i casi deve comparire subito nella tabella.
        _listaAbbonamentiMaster.Insert(0, nuovo);
        AggiornaAnniDisponibili();
        PaginaCorrente = 1;
        ApplicaFiltroEPaginazione();

        if (_stampaRicevutaCortesiaAttiva)
        {
            string messaggio = Allievo.Id > 0
                ? $"Abbonamento registrato con successo!\nScadenza: {nuovo.DataScadenza:dd/MM/yyyy}."
                : $"Abbonamento aggiunto.\nScadenza: {nuovo.DataScadenza:dd/MM/yyyy}.\n\n⚠️ Verrà salvato insieme all'allievo quando premi «Salva».";

            bool vuoleStampare = await AlertPopup.ShowConfirmation(
                "Abbonamento Creato",
                $"{messaggio}\n\nVuoi stampare la ricevuta di cortesia?",
                "Sì, Stampa",
                "No");

            if (vuoleStampare)
            {
                await StampaRicevutaAsync(nuovo);
            }
        }
    }

    // ⏯️ PLAY / STOP
    [RelayCommand]
    public async Task ToggleSospensioneAbbonamentoAsync(Abbonamenti abbonamento)
    {
        if (abbonamento == null) return;

        if (abbonamento.IsSospeso == 0)
        {
            bool confermaStop = await AlertPopup.ShowConfirmation(
                "Sospendi Abbonamento",
                $"Sei sicuro di voler sospendere l'abbonamento '{abbonamento.Corso?.Nome ?? abbonamento.TipoAbbonamento}'? I giorni rimanenti verranno congelati.",
                "Sì, Sospendi",
                "Annulla");

            if (!confermaStop) return;

            abbonamento.IsSospeso = 1;
            abbonamento.DataSospensione = DateTime.Now;
            var giorniRimanenti = (abbonamento.DataScadenza.Date - DateTime.Now.Date).Days;
            abbonamento.GiorniRimanentiCongelati = Math.Max(0, giorniRimanenti);
        }
        else
        {
            DateTime nuovaScadenza = DateTime.Now.AddDays(abbonamento.GiorniRimanentiCongelati);

            bool confermaPlay = await AlertPopup.ShowConfirmation(
                "Ripristina Abbonamento",
                $"Vuoi riattivare l'abbonamento? La nuova data di scadenza sarà il {nuovaScadenza:dd/MM/yyyy}.",
                "Sì, Riattiva",
                "Annulla");

            if (!confermaPlay) return;

            abbonamento.IsSospeso = 0;
            abbonamento.DataScadenza = nuovaScadenza;
            abbonamento.DataSospensione = null;
            abbonamento.GiorniRimanentiCongelati = 0;
        }

        if (abbonamento.Id > 0)
        {
            await _dbService.SalvaAbbonamentoAsync(abbonamento);
        }

        ApplicaFiltroEPaginazione();
    }

    // 🔄 RINNOVA ABBONAMENTO
    // Ogni rinnovo è un NUOVO record: l'incasso resta registrato e lo storico
    // dei pagamenti rimane consultabile. Stessa regola del pagamento rapido
    // dal calendario.
    [RelayCommand]
    public async Task RinnovaAbbonamentoAsync(Abbonamenti abbonamento)
    {
        if (abbonamento == null) return;

        int giorniAggiunti = abbonamento.TipoAbbonamento switch
        {
            "Singolo" => 1,
            "Annuale" => 365,
            _ => 28
        };

        // Se l'abbonamento è ancora valido il rinnovo parte dalla sua scadenza,
        // altrimenti da oggi.
        bool isGiaAttivo = abbonamento.DataScadenza.Date >= DateTime.Today;
        DateTime dataInizio = isGiaAttivo ? abbonamento.DataScadenza : DateTime.Now;
        DateTime nuovaScadenza = dataInizio.AddDays(giorniAggiunti);

        // Il corso è sempre valorizzato: GetAbbonamentiAllievoAsync fa Include(a => a.Corso).
        var corso = abbonamento.Corso;

        double importo = abbonamento.TipoAbbonamento switch
        {
            "Singolo" => corso?.CostoSingolo ?? 0,
            "Annuale" => corso?.CostoAnnuale ?? 0,
            _ => corso?.CostoMensile ?? 0
        };

        // Se il corso non ha più un prezzo impostato, si riusa quello dell'ultimo pagamento
        if (importo <= 0) importo = abbonamento.ImportoTotale;

        bool conferma = await AlertPopup.ShowConfirmation(
            "Conferma Rinnovo",
            $"Vuoi registrare un nuovo pagamento di € {importo:N2} per '{corso?.Nome ?? abbonamento.TipoAbbonamento}'?\n\nValidità: dal {dataInizio:dd/MM/yyyy} al {nuovaScadenza:dd/MM/yyyy}.",
            "Sì, Rinnova",
            "Annulla");

        if (!conferma) return;

        var rinnovo = new Abbonamenti
        {
            AllievoId = abbonamento.AllievoId,
            Allievo = this.Allievo,
            CorsoId = abbonamento.CorsoId,
            Corso = corso!,
            TipoAbbonamento = abbonamento.TipoAbbonamento,
            DataInizio = dataInizio,
            DataScadenza = nuovaScadenza,
            ImportoTotale = importo,
            ImportoPagato = importo,
            IsPagato = 1,
            IsSospeso = 0,
            Attivo = 1
        };

        await EseguiConCaricamento(async () =>
        {
            await _dbService.SalvaAbbonamentoAsync(rinnovo);

            _listaAbbonamentiMaster.Insert(0, rinnovo);
            AggiornaAnniDisponibili();
            PaginaCorrente = 1;
            ApplicaFiltroEPaginazione();
        });

        if (_stampaRicevutaCortesiaAttiva)
        {
            bool vuoleStampare = await AlertPopup.ShowConfirmation(
                "Rinnovato",
                $"Pagamento di € {importo:N2} registrato.\nNuova scadenza: {nuovaScadenza:dd/MM/yyyy}.\n\nVuoi stampare la ricevuta di cortesia?",
                "Sì, Stampa",
                "No");

            if (vuoleStampare)
            {
                await StampaRicevutaAsync(rinnovo);
            }
        }
    }

    // ➕ AGGIUNGI 1 SETTIMANA (recupero lezione persa)
    [RelayCommand]
    public Task AumentaSettimanaAbbonamentoAsync(Abbonamenti abbonamento) =>
        SpostaScadenzaAsync(abbonamento, 7);

    // ➖ TOGLI 1 SETTIMANA (correzione manuale della scadenza)
    [RelayCommand]
    public Task DiminuisciSettimanaAbbonamentoAsync(Abbonamenti abbonamento) =>
        SpostaScadenzaAsync(abbonamento, -7);

    private async Task SpostaScadenzaAsync(Abbonamenti abbonamento, int giorni)
    {
        if (abbonamento == null) return;

        DateTime nuovaScadenza = abbonamento.DataScadenza.AddDays(giorni);

        // Togliere giorni è un'operazione distruttiva: si chiede conferma prima.
        // Aggiungerli no: si avvisa dopo, a cose fatte.
        if (giorni < 0)
        {
            bool conferma = await AlertPopup.ShowConfirmation(
                "Togli 1 Settimana",
                $"Vuoi anticipare la scadenza di {Math.Abs(giorni)} giorni?\nNuova scadenza: {nuovaScadenza:dd/MM/yyyy}",
                "Sì, Togli",
                "Annulla");

            if (!conferma) return;
        }

        abbonamento.DataScadenza = nuovaScadenza;

        if (abbonamento.Id > 0)
        {
            await _dbService.SalvaAbbonamentoAsync(abbonamento);
        }

        ApplicaFiltroEPaginazione();

        if (giorni > 0)
        {
            await AlertPopup.Show(
                "Settimana di Recupero Aggiunta",
                $"Nuova scadenza: {abbonamento.DataScadenza:dd/MM/yyyy}.",
                "OK");
        }
    }

    // 🗑️ ELIMINA ABBONAMENTO
    [RelayCommand]
    public async Task EliminaAbbonamentoAsync(Abbonamenti abbonamento)
    {
        if (abbonamento == null) return;

        bool conferma = await AlertPopup.ShowConfirmation("Elimina", "Vuoi cancellare questo abbonamento dallo storico?", "Sì, Elimina", "Annulla");
        if (conferma)
        {
            if (abbonamento.Id > 0)
            {
                await _dbService.EliminaAbbonamentoAsync(abbonamento.Id);
            }
            _listaAbbonamentiMaster.Remove(abbonamento);
            AggiornaAnniDisponibili();
            ApplicaFiltroEPaginazione();
        }
    }

    // 📋 STORICO ABBONAMENTI DEL CORSO
    // In tabella si vede solo l'ultimo abbonamento di ogni corso: da qui si apre
    // l'elenco completo dei pagamenti di quell'allievo per quel corso.
    [RelayCommand]
    public async Task ApriStoricoAbbonamentiAsync(Abbonamenti abbonamento)
    {
        if (abbonamento == null) return;

        var storico = _listaAbbonamentiMaster
            .Where(a => a.CorsoId == abbonamento.CorsoId)
            .ToList();

        string nomeCorso = abbonamento.Corso?.Nome ?? abbonamento.TipoAbbonamento;
        string nomeAllievo = string.IsNullOrWhiteSpace(Allievo.NomeCompleto)
            ? $"{Nome} {Cognome}".Trim()
            : Allievo.NomeCompleto;

        var popup = new StoricoAbbonamentiPage(nomeCorso, nomeAllievo, storico);
        await Shell.Current.Navigation.PushModalAsync(popup);
    }

    // 🖨️ STAMPA RICEVUTA
    [RelayCommand]
    public async Task StampaRicevutaAsync(Abbonamenti abbonamento)
    {
        if (abbonamento == null) return;

        if (Allievo.Id == 0)
        {
            SincronizzaAllievoDaCampi();

            if (!string.IsNullOrWhiteSpace(Allievo.Nome) && !string.IsNullOrWhiteSpace(Allievo.Cognome))
            {
                await _dbService.SalvaAllievoAsync(Allievo);

                if (abbonamento.AllievoId == 0)
                {
                    abbonamento.AllievoId = Allievo.Id;
                }
            }
        }

        abbonamento.Allievo ??= Allievo;

        // Si sta stampando una ricevuta, quindi l'abbonamento è stato pagato:
        // se non è ancora a database va salvato adesso, non alla chiusura scheda.
        if (abbonamento.Id == 0 && Allievo.Id > 0)
        {
            await _dbService.SalvaAbbonamentoAsync(abbonamento);
        }

        await _ricevutaService.StampaRicevutaCortesiaAsync(abbonamento);
    }

    private void SincronizzaAllievoDaCampi()
    {
        Allievo ??= new Allievi();

        Allievo.Nome = Nome.Trim();
        Allievo.Cognome = Cognome.Trim();
        Allievo.CodiceFiscale = CodiceFiscale?.Trim().ToUpper();
        Allievo.Telefono = Telefono?.Trim();
        Allievo.Email = Email?.Trim();

        Allievo.Sesso = Sesso;
        Allievo.DataNascita = DataNascita.ToString("yyyy-MM-dd");
        Allievo.Indirizzo = Indirizzo?.Trim();
        Allievo.NCivico = NCivico?.Trim();
        Allievo.Cap = Cap?.Trim();
        Allievo.Citta = Citta?.Trim();
        Allievo.Provincia = Provincia?.Trim();
        Allievo.Cellulare = Cellulare?.Trim();
        Allievo.Allegati = Allegati?.Trim();
    }

    [RelayCommand]
    public async Task SalvaAllievoAsync()
    {
        if (string.IsNullOrWhiteSpace(Nome) || string.IsNullOrWhiteSpace(Cognome))
        {
            await AlertPopup.Show("Attenzione", "Inserisci sia il nome che il cognome.", "OK");
            return;
        }

        await EseguiConCaricamento(async () =>
        {
            SincronizzaAllievoDaCampi();

            await _dbService.SalvaAllievoAsync(Allievo);

            // Salva gli abbonamenti non ancora presenti a database (Id == 0),
            // cioè quelli aggiunti su una scheda allievo nuova.
            // Il criterio è Id e non AllievoId: la stampa ricevuta può aver già
            // valorizzato AllievoId senza però salvare l'abbonamento.
            foreach (var abb in _listaAbbonamentiMaster.Where(a => a.Id == 0))
            {
                abb.AllievoId = Allievo.Id;
                await _dbService.SalvaAbbonamentoAsync(abb);
            }
        });

        await Shell.Current.Navigation.PopAsync();
    }

    // 🔒 DOCUMENTO PRIVACY (ora su bottone dedicato, non più automatico al salvataggio)
    [RelayCommand]
    public async Task ApriDocumentoPrivacyAsync()
    {
        if (string.IsNullOrWhiteSpace(Nome) || string.IsNullOrWhiteSpace(Cognome))
        {
            await AlertPopup.Show("Attenzione", "Inserisci sia il nome che il cognome.", "OK");
            return;
        }

        SincronizzaAllievoDaCampi();

        // Propone solo i modelli effettivamente presenti nella cartella Privacy
        var opzioni = new List<string>();
        if (PrivacyDocumentService.ModelloCompilatoDisponibile) opzioni.Add("Documento Compilato");
        if (PrivacyDocumentService.ModelloVuotoDisponibile) opzioni.Add("Modulo Vuoto");

        if (opzioni.Count == 0)
        {
            await AlertPopup.Show(
                "Modelli non presenti",
                "Non è stato inserito nessun modello privacy nella cartella Privacy dell'applicazione. Vedi il file LEGGIMI.txt.",
                "OK");
            return;
        }

        // Con un solo modello disponibile è inutile far scegliere
        string scelta = opzioni.Count == 1
            ? opzioni[0]
            : await AlertPopup.ShowActionSheet("Documento Privacy - Come lo vuoi?", "Annulla", null, opzioni.ToArray());

        if (scelta == "Documento Compilato")
        {
            await _privacyDocumentService.GeneraECondividiDocumentoCompilatoAsync(Allievo);
        }
        else if (scelta == "Modulo Vuoto")
        {
            await _privacyDocumentService.ApriModuloVuotoAsync();
        }
    }

    // ✍️ FIRMA DEL MODULO PRIVACY
    // La firma è a penna sulla carta: qui la segreteria registra che è avvenuta,
    // e in quel momento nasce il PDF da conservare in archivio.
    [RelayCommand]
    public async Task ConfermaFirmaPrivacyAsync()
    {
        if (Allievo.Id == 0)
        {
            await AlertPopup.Show("Attenzione", "Salva prima l'allievo.", "OK");
            return;
        }

        bool conferma = await AlertPopup.ShowConfirmation(
            "Modulo firmato",
            $"Confermi che {Nome} {Cognome} ha firmato il modulo privacy?\n\nVerrà archiviata una copia in PDF.",
            "Sì, è firmato",
            "Annulla");

        if (!conferma) return;

        await EseguiConCaricamento(async () =>
        {
            SincronizzaAllievoDaCampi();

            var privacy = _privacyCorrente ?? new Privacy { IdAllievo = Allievo.Id };

            privacy.IdAllievo = Allievo.Id;
            privacy.Firmato = 1;
            privacy.DataFirma = DateTime.Now;
            privacy.Data ??= DateTime.Now.ToString("yyyy-MM-dd");
            privacy.PresaVisione = 1;
            privacy.Attivo = 1;
            // Quale dei due modelli (a penna o dispositivo) è stato firmato davvero.
            privacy.ModelloUsato = await _privacyDocumentService.NomeModelloInUsoAsync();

            string? percorsoPdf = await _privacyDocumentService.ArchiviaModuloFirmatoAsync(Allievo);
            if (!string.IsNullOrWhiteSpace(percorsoPdf))
            {
                privacy.PercorsoPdf = percorsoPdf;
            }

            await _dbService.SalvaPrivacyAsync(privacy);
            _privacyCorrente = privacy;

            await CaricaStatoPrivacyAsync();

            if (string.IsNullOrWhiteSpace(percorsoPdf))
            {
                // La firma è registrata lo stesso: il PDF si può rigenerare dopo.
                await AlertPopup.Show(
                    "Firma registrata",
                    "La firma è stata registrata, ma la copia PDF non è stata creata.\n\nControlla che in Impostazioni sia indicata la cartella di archivio dei moduli.",
                    "OK");
            }
        });
    }

    [RelayCommand]
    public async Task ApriPdfPrivacyAsync()
    {
        string? percorso = _privacyCorrente?.PercorsoPdf;

        if (string.IsNullOrWhiteSpace(percorso) || !File.Exists(percorso))
        {
            await AlertPopup.Show("Attenzione", "La copia PDF non è più disponibile nel percorso registrato.", "OK");
            return;
        }

        await Launcher.Default.OpenAsync(new OpenFileRequest
        {
            Title = "Modulo Privacy Firmato",
            File = new ReadOnlyFile(percorso)
        });
    }

    [RelayCommand]
    public async Task AnnullaFirmaPrivacyAsync()
    {
        if (_privacyCorrente == null) return;

        bool conferma = await AlertPopup.ShowConfirmation(
            "Annulla firma",
            "Vuoi togliere la spunta di firma?\n\nIl PDF già archiviato resta nella cartella: va eliminato a mano se non serve più.",
            "Sì, togli",
            "Annulla");

        if (!conferma) return;

        await EseguiConCaricamento(async () =>
        {
            _privacyCorrente.Firmato = 0;
            _privacyCorrente.DataFirma = null;

            await _dbService.SalvaPrivacyAsync(_privacyCorrente);
            await CaricaStatoPrivacyAsync();
        });
    }

    [RelayCommand]
    public async Task EliminaAllievoAsync()
    {
        if (Allievo == null || Allievo.Id == 0) return;

        bool conferma = await AlertPopup.ShowConfirmation(
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