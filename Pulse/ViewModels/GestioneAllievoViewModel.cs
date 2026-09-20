using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pulse.Models;
using Pulse.Services;
using System.Collections.ObjectModel;
using Pulse.Views.Popups;

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

            var anni = _listaAbbonamentiMaster
                .Select(a => a.DataInizio.Year.ToString())
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();

            anni.Insert(0, "Tutti gli anni");
            AnniDisponibili = new ObservableCollection<string>(anni);

            // Seleziona esplicitamente il primo valore per far apparire la scritta
            AnnoSelezionato = AnniDisponibili.FirstOrDefault() ?? "Tutti gli anni";

            PaginaCorrente = 1;
            ApplicaFiltroEPaginazione();
        }
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

        var listaFiltrata = filtrati.ToList();

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
            await Shell.Current.DisplayAlert("Attenzione", "Prima devi creare un corso!", "OK");
            return;
        }

        var opzioniCorsi = corsiDisponibili.Select(c => c.Nome).ToArray();
        string corsoSelezionatoNome = await Shell.Current.DisplayActionSheet("Seleziona il Corso:", "Annulla", null, opzioniCorsi);

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

        if (Allievo.Id > 0)
        {
            await _dbService.SalvaAbbonamentoAsync(nuovo);
            _listaAbbonamentiMaster.Insert(0, nuovo);
            PaginaCorrente = 1;
            ApplicaFiltroEPaginazione();
        }

        if (_stampaRicevutaCortesiaAttiva)
        {
            bool vuoleStampare = await Shell.Current.DisplayAlert(
                "Abbonamento Creato",
                $"Abbonamento registrato con successo!\nScadenza: {nuovo.DataScadenza:dd/MM/yyyy}.\n\nVuoi stampare la ricevuta di cortesia?",
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
            bool confermaStop = await Shell.Current.DisplayAlert(
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

            bool confermaPlay = await Shell.Current.DisplayAlert(
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

        double importo = abbonamento.TipoAbbonamento switch
        {
            "Singolo" => abbonamento.Corso?.CostoSingolo ?? 0,
            "Annuale" => abbonamento.Corso?.CostoAnnuale ?? 0,
            _ => abbonamento.Corso?.CostoMensile ?? 0
        };

        // Se il corso non ha più un prezzo impostato, si riusa quello dell'ultimo pagamento
        if (importo <= 0) importo = abbonamento.ImportoTotale;

        bool conferma = await Shell.Current.DisplayAlert(
            "Conferma Rinnovo",
            $"Vuoi registrare un nuovo pagamento di € {importo:N2} per '{abbonamento.Corso?.Nome ?? abbonamento.TipoAbbonamento}'?\n\nValidità: dal {dataInizio:dd/MM/yyyy} al {nuovaScadenza:dd/MM/yyyy}.",
            "Sì, Rinnova",
            "Annulla");

        if (!conferma) return;

        var rinnovo = new Abbonamenti
        {
            AllievoId = abbonamento.AllievoId,
            Allievo = this.Allievo,
            CorsoId = abbonamento.CorsoId,
            Corso = abbonamento.Corso,
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
            PaginaCorrente = 1;
            ApplicaFiltroEPaginazione();
        });

        if (_stampaRicevutaCortesiaAttiva)
        {
            bool vuoleStampare = await Shell.Current.DisplayAlert(
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
            bool conferma = await Shell.Current.DisplayAlert(
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
            await Shell.Current.DisplayAlert(
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

        bool conferma = await Shell.Current.DisplayAlert("Elimina", "Vuoi cancellare questo abbonamento dallo storico?", "Sì, Elimina", "Annulla");
        if (conferma)
        {
            if (abbonamento.Id > 0)
            {
                await _dbService.EliminaAbbonamentoAsync(abbonamento.Id);
            }
            _listaAbbonamentiMaster.Remove(abbonamento);
            ApplicaFiltroEPaginazione();
        }
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
            await Shell.Current.DisplayAlert("Attenzione", "Inserisci sia il nome che il cognome.", "OK");
            return;
        }

        await EseguiConCaricamento(async () =>
        {
            SincronizzaAllievoDaCampi();

            await _dbService.SalvaAllievoAsync(Allievo);

            // Solo gli abbonamenti creati prima che l'allievo avesse un Id
            // hanno bisogno di essere salvati adesso, con il collegamento corretto.
            foreach (var abb in _listaAbbonamentiMaster.Where(a => a.AllievoId == 0))
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
            await Shell.Current.DisplayAlert("Attenzione", "Inserisci sia il nome che il cognome.", "OK");
            return;
        }

        SincronizzaAllievoDaCampi();

        // Propone solo i modelli effettivamente presenti nella cartella Privacy
        var opzioni = new List<string>();
        if (PrivacyDocumentService.ModelloCompilatoDisponibile) opzioni.Add("Documento Compilato");
        if (PrivacyDocumentService.ModelloVuotoDisponibile) opzioni.Add("Modulo Vuoto");

        if (opzioni.Count == 0)
        {
            await Shell.Current.DisplayAlert(
                "Modelli non presenti",
                "Non è stato inserito nessun modello privacy nella cartella Privacy dell'applicazione. Vedi il file LEGGIMI.txt.",
                "OK");
            return;
        }

        // Con un solo modello disponibile è inutile far scegliere
        string scelta = opzioni.Count == 1
            ? opzioni[0]
            : await Shell.Current.DisplayActionSheet("Documento Privacy - Come lo vuoi?", "Annulla", null, opzioni.ToArray());

        if (scelta == "Documento Compilato")
        {
            await _privacyDocumentService.GeneraECondividiDocumentoCompilatoAsync(Allievo);
        }
        else if (scelta == "Modulo Vuoto")
        {
            await _privacyDocumentService.ApriModuloVuotoAsync();
        }
    }

    [RelayCommand]
    public async Task EliminaAllievoAsync()
    {
        if (Allievo == null || Allievo.Id == 0) return;

        bool conferma = await Shell.Current.DisplayAlert(
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