using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pulse.Models;
using Pulse.Services;
using System.Collections.ObjectModel;

namespace Pulse.ViewModels;

[QueryProperty(nameof(Allievo), "Allievo")]
public partial class GestioneAllievoViewModel : BaseViewModel
{
    private readonly IDatabaseService _dbService;
    private readonly RicevutaService _ricevutaService;
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

    [ObservableProperty]
    private bool _isEdizione = false;

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

    public GestioneAllievoViewModel(INavigationService navigationService, IDatabaseService dbService)
        : base(navigationService)
    {
        _dbService = dbService;
        _ricevutaService = new RicevutaService();
        Title = "Gestione Allievo";
    }

    partial void OnAllievoChanged(Allievi value)
    {
        if (value == null) return;

        Nome = value.Nome ?? string.Empty;
        Cognome = value.Cognome ?? string.Empty;
        CodiceFiscale = value.CodiceFiscale ?? string.Empty;
        Telefono = value.Telefono ?? string.Empty;
        Email = value.Email ?? string.Empty;

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

        var opzioniPrezzi = new List<string>();
        if (corsoScelto.CostoSingolo.HasValue && corsoScelto.CostoSingolo.Value > 0)
            opzioniPrezzi.Add($"Singolo (€ {corsoScelto.CostoSingolo.Value:N2})");
        if (corsoScelto.CostoMensile.HasValue && corsoScelto.CostoMensile.Value > 0)
            opzioniPrezzi.Add($"Mensile 4 Settimane (€ {corsoScelto.CostoMensile.Value:N2})");
        if (corsoScelto.CostoAnnuale.HasValue && corsoScelto.CostoAnnuale.Value > 0)
            opzioniPrezzi.Add($"Annuale (€ {corsoScelto.CostoAnnuale.Value:N2})");

        if (opzioniPrezzi.Count == 0)
        {
            opzioniPrezzi.Add("Mensile 4 Settimane (€ 0,00)");
        }

        string tipoSelezionato = await Shell.Current.DisplayActionSheet(
            $"Abbonamento per {corsoScelto.Nome}:",
            "Annulla",
            null,
            opzioniPrezzi.ToArray());

        if (string.IsNullOrEmpty(tipoSelezionato) || tipoSelezionato == "Annulla") return;

        string tipo = "Mensile";
        double importo = corsoScelto.CostoMensile ?? 0;
        DateTime scadenza = DateTime.Now.AddDays(28);

        if (tipoSelezionato.StartsWith("Singolo"))
        {
            tipo = "Singolo";
            importo = corsoScelto.CostoSingolo ?? 0;
            scadenza = DateTime.Now.AddDays(1);
        }
        else if (tipoSelezionato.StartsWith("Annuale"))
        {
            tipo = "Annuale";
            importo = corsoScelto.CostoAnnuale ?? 0;
            scadenza = DateTime.Now.AddYears(1);
        }

        var nuovo = new Abbonamenti
        {
            AllievoId = Allievo.Id,
            Allievo = Allievo,
            CorsoId = corsoScelto.Id,
            Corso = corsoScelto,
            TipoAbbonamento = tipo,
            DataInizio = DateTime.Now,
            DataScadenza = scadenza,
            ImportoTotale = importo,
            ImportoPagato = importo,
            IsPagato = 1,
            IsSospeso = 0,
            Attivo = 1
        };

        if (Allievo.Id > 0)
        {
            await _dbService.SalvaAbbonamentoAsync(nuovo);
            _listaAbbonamentiMaster.Insert(0, nuovo);
            PaginaCorrente = 1;
            ApplicaFiltroEPaginazione();
        }

        bool vuoleStampare = await Shell.Current.DisplayAlert(
            "Abbonamento Creato",
            $"Abbonamento registrato con successo!\nScadenza: {scadenza:dd/MM/yyyy}.\n\nVuoi stampare la ricevuta di cortesia?",
            "Sì, Stampa",
            "No");

        if (vuoleStampare)
        {
            await StampaRicevutaAsync(nuovo);
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

        bool isGiaAttivo = abbonamento.DataScadenza >= DateTime.Now.Date;
        DateTime baseData = isGiaAttivo ? abbonamento.DataScadenza : DateTime.Now;
        DateTime nuovaScadenzaCalcolata = baseData.AddDays(giorniAggiunti);

        bool conferma = await Shell.Current.DisplayAlert(
            "Conferma Rinnovo",
            $"Vuoi rinnovare l'abbonamento fino al {nuovaScadenzaCalcolata:dd/MM/yyyy}?",
            "Sì, Rinnova",
            "Annulla");

        if (!conferma) return;

        abbonamento.DataScadenza = nuovaScadenzaCalcolata;
        abbonamento.IsSospeso = 0;
        abbonamento.DataSospensione = null;
        abbonamento.GiorniRimanentiCongelati = 0;

        if (abbonamento.Id > 0)
        {
            await _dbService.SalvaAbbonamentoAsync(abbonamento);
        }

        ApplicaFiltroEPaginazione();

        bool vuoleStampare = await Shell.Current.DisplayAlert(
            "Rinnovato",
            $"Abbonamento rinnovato fino al {abbonamento.DataScadenza:dd/MM/yyyy}!\n\nVuoi stampare la ricevuta di cortesia?",
            "Sì, Stampa",
            "No");

        if (vuoleStampare)
        {
            await StampaRicevutaAsync(abbonamento);
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

        abbonamento.Allievo ??= Allievo;

        string nomeScuola = Preferences.Get("Scuola_Nome", "ASD SCUOLA DI DANZA PULSE");
        string indirizzoScuola = Preferences.Get("Scuola_Indirizzo", "Via Roma 123 - San Benedetto del Tronto (AP)");
        string pivaScuola = Preferences.Get("Scuola_PIVA", "01234567890");

        await _ricevutaService.StampaRicevutaCortesiaAsync(abbonamento, nomeScuola, indirizzoScuola, pivaScuola);
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
            Allievo ??= new Allievi();

            Allievo.Nome = Nome.Trim();
            Allievo.Cognome = Cognome.Trim();
            Allievo.CodiceFiscale = CodiceFiscale?.Trim().ToUpper();
            Allievo.Telefono = Telefono?.Trim();
            Allievo.Email = Email?.Trim();

            await _dbService.SalvaAllievoAsync(Allievo);

            foreach (var abb in _listaAbbonamentiMaster)
            {
                if (abb.AllievoId == 0) abb.AllievoId = Allievo.Id;
                await _dbService.SalvaAbbonamentoAsync(abb);
            }

            await Shell.Current.Navigation.PopAsync();
        });
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