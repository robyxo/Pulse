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

    [ObservableProperty]
    private ObservableCollection<Abbonamenti> _listaAbbonamenti = new();

    public GestioneAllievoViewModel(INavigationService navigationService, IDatabaseService dbService)
        : base(navigationService)
    {
        _dbService = dbService;
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

    private async Task CaricaAbbonamentiAsync()
    {
        if (Allievo.Id > 0)
        {
            var abbonamenti = await _dbService.GetAbbonamentiAllievoAsync(Allievo.Id);
            ListaAbbonamenti = new ObservableCollection<Abbonamenti>(abbonamenti);
        }
    }

    // 🟢 NUOVO ABBONAMENTO CON VERIFICA CORSI ESISTENTI
    [RelayCommand]
    public async Task ApriPopupNuovoAbbonamentoAsync()
    {
        var corsiDisponibili = await _dbService.GetCorsiAttiviAsync();

        // Se non ci sono corsi registrati, avvisa l'utente
        if (corsiDisponibili == null || corsiDisponibili.Count == 0)
        {
            await Shell.Current.DisplayAlert("Attenzione", "Prima devi creare un corso!", "OK");
            return;
        }

        // 1. Scelta del Corso
        var opzioniCorsi = corsiDisponibili.Select(c => c.Nome).ToArray();
        string corsoSelezionatoNome = await Shell.Current.DisplayActionSheet("Seleziona il Corso:", "Annulla", null, opzioniCorsi);

        if (string.IsNullOrEmpty(corsoSelezionatoNome) || corsoSelezionatoNome == "Annulla") return;

        var corsoScelto = corsiDisponibili.FirstOrDefault(c => c.Nome == corsoSelezionatoNome);
        if (corsoScelto == null) return;

        // 2. Scelta del Tipo di Abbonamento con i prezzi definiti nel corso
        var opzioniPrezzi = new List<string>();
        if (corsoScelto.CostoSingolo.HasValue && corsoScelto.CostoSingolo.Value > 0)
            opzioniPrezzi.Add($"Singolo (€ {corsoScelto.CostoSingolo.Value:N2})");
        if (corsoScelto.CostoMensile.HasValue && corsoScelto.CostoMensile.Value > 0)
            opzioniPrezzi.Add($"Mensile 4 Settimane (€ {corsoScelto.CostoMensile.Value:N2})");
        if (corsoScelto.CostoAnnuale.HasValue && corsoScelto.CostoAnnuale.Value > 0)
            opzioniPrezzi.Add($"Annuale (€ {corsoScelto.CostoAnnuale.Value:N2})");

        if (opzioniPrezzi.Count == 0)
        {
            // Fallback se nessun prezzo è impostato
            opzioniPrezzi.Add("Mensile 4 Settimane (€ 0,00)");
        }

        string tipoSelezionato = await Shell.Current.DisplayActionSheet(
            $"Abbonamento per {corsoScelto.Nome}:",
            "Annulla",
            null,
            opzioniPrezzi.ToArray());

        if (string.IsNullOrEmpty(tipoSelezionato) || tipoSelezionato == "Annulla") return;

        // 3. Calcolo Scadenza e Costo
        string tipo = "Mensile";
        double importo = corsoScelto.CostoMensile ?? 0;
        DateTime scadenza = DateTime.Now.AddDays(28); // 4 Settimane

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
            CorsoId = corsoScelto.Id,
            Corso = corsoScelto,
            TipoAbbonamento = tipo,
            DataInizio = DateTime.Now,
            DataScadenza = scadenza,
            ImportoTotale = importo,
            ImportoPagato = importo,
            IsPagato = 1,
            IsSospeso = 0
        };

        if (Allievo.Id > 0)
        {
            await _dbService.SalvaAbbonamentoAsync(nuovo);
        }

        ListaAbbonamenti.Insert(0, nuovo);
    }

    // ⏯️ PLAY / STOP (Pausa e Ripristino Sospensione)
    [RelayCommand]
    public async Task ToggleSospensioneAbbonamentoAsync(Abbonamenti abbonamento)
    {
        if (abbonamento == null) return;

        if (abbonamento.IsSospeso == 0)
        {
            // ⏸️ RICHIESTA CONFERMA SOSPENSIONE
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

            await Shell.Current.DisplayAlert("Abbonamento Sospeso", $"Abbonamento congelato con {abbonamento.GiorniRimanentiCongelati} giorni rimanenti.", "OK");
        }
        else
        {
            // ▶️ RICHIESTA CONFERMA RIPRISTINO
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

            await Shell.Current.DisplayAlert("Abbonamento Riattivato", $"Abbonamento ripristinato. Nuova scadenza: {abbonamento.DataScadenza:dd/MM/yyyy}", "OK");
        }

        if (abbonamento.Id > 0)
        {
            await _dbService.SalvaAbbonamentoAsync(abbonamento);
        }

        // 🟢 Forza il refresh immediato della lista per aggiornare badge, colori e icona
        int index = ListaAbbonamenti.IndexOf(abbonamento);
        if (index >= 0)
        {
            ListaAbbonamenti.RemoveAt(index);
            ListaAbbonamenti.Insert(index, abbonamento);
        }
    }

    // 🔄 RINNOVA ABBONAMENTO CON CONFERMA PREVENTIVA
    [RelayCommand]
    public async Task RinnovaAbbonamentoAsync(Abbonamenti abbonamento)
    {
        if (abbonamento == null) return;

        // 1. Determina il periodo da aggiungere e l'etichetta del messaggio
        int giorniAggiunti;
        string unitaTempo;

        switch (abbonamento.TipoAbbonamento)
        {
            case "Singolo":
                giorniAggiunti = 1;
                unitaTempo = "un'ulteriore lezione/giorno";
                break;
            case "Annuale":
                giorniAggiunti = 365;
                unitaTempo = "un ulteriore anno";
                break;
            default: // Mensile = 4 settimane (28 giorni)
                giorniAggiunti = 28;
                unitaTempo = "un ulteriore mese (4 settimane)";
                break;
        }

        // 2. Calcola la data di partenza: se è scaduto parte da oggi, se attivo si accoda alla scadenza attuale
        bool isGiaAttivo = abbonamento.DataScadenza >= DateTime.Now.Date;
        DateTime baseData = isGiaAttivo ? abbonamento.DataScadenza : DateTime.Now;
        DateTime nuovaScadenzaCalcolata = baseData.AddDays(giorniAggiunti);

        // 3. Mostra la richiesta di conferma
        string messaggioConferma = isGiaAttivo
            ? $"L'abbonamento è attualmente attivo.\n\nSei sicuro di voler aggiungere {unitaTempo} all'abbonamento attuale?\nLa nuova scadenza è prevista per il {nuovaScadenzaCalcolata:dd/MM/yyyy}.\n\nProcedere?"
            : $"L'abbonamento è scaduto.\n\nVuoi rinnovarlo per {unitaTempo} a partire da oggi?\nNuova scadenza: {nuovaScadenzaCalcolata:dd/MM/yyyy}.\n\nProcedere?";

        bool conferma = await Shell.Current.DisplayAlert(
            "Conferma Rinnovo",
            messaggioConferma,
            "Sì, Rinnova",
            "Annulla");

        if (!conferma) return;

        // 4. Applica il rinnovo e riattiva l'abbonamento se era sospeso
        abbonamento.DataScadenza = nuovaScadenzaCalcolata;
        abbonamento.IsSospeso = 0;
        abbonamento.DataSospensione = null;
        abbonamento.GiorniRimanentiCongelati = 0;

        if (abbonamento.Id > 0)
        {
            await _dbService.SalvaAbbonamentoAsync(abbonamento);
        }

        // 5. Ricarica la lista per aggiornare subito badge, colori e date a video
        if (Allievo.Id > 0)
        {
            var abbonamentiAggiornati = await _dbService.GetAbbonamentiAllievoAsync(Allievo.Id);
            ListaAbbonamenti = new ObservableCollection<Abbonamenti>(abbonamentiAggiornati);
        }
        else
        {
            var index = ListaAbbonamenti.IndexOf(abbonamento);
            if (index >= 0)
            {
                ListaAbbonamenti.RemoveAt(index);
                ListaAbbonamenti.Insert(index, abbonamento);
            }
        }

        await Shell.Current.DisplayAlert("Rinnovato", $"Abbonamento rinnovato con successo fino al {abbonamento.DataScadenza:dd/MM/yyyy}!", "OK");
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
            ListaAbbonamenti.Remove(abbonamento);
        }
    }

    // 🖨️ STAMPA RICEVUTA DI CORTESIA
    [RelayCommand]
    public async Task StampaRicevutaAsync(Abbonamenti abbonamento)
    {
        if (abbonamento == null) return;

        abbonamento.Allievo ??= Allievo;

        string nomeScuola = Preferences.Get("Scuola_Nome", "ASD SCUOLA DI DANZA PULSE");
        string indirizzoScuola = Preferences.Get("Scuola_Indirizzo", "Via Roma 123 - San Benedetto del Tronto (AP)");
        string pivaScuola = Preferences.Get("Scuola_PIVA", "01234567890");

        string numRicevuta = $"{abbonamento.Id:D5}";
        string dataOggi = DateTime.Now.ToString("dd/MM/yyyy");
        string periodo = $"{abbonamento.DataInizio:dd/MM/yyyy} al {abbonamento.DataScadenza:dd/MM/yyyy}";

        string ricevutaTesto =
$@"--------------------------------------------------
         {nomeScuola.ToUpper()}
         {indirizzoScuola}
         P.IVA / C.F.: {pivaScuola}
--------------------------------------------------
RICEVUTA DI CORTESIA N. {numRicevuta} del {dataOggi}
(Valido solo come attestazione di pagamento)

Allievo: {abbonamento.Allievo.Nome} {abbonamento.Allievo.Cognome}
C.F.: {abbonamento.Allievo.CodiceFiscale}

Somma Pagata: € {abbonamento.ImportoTotale:N2}
Corso: {abbonamento.Corso?.Nome ?? "Corso Danza"}
Tipo: Abbonamento {abbonamento.TipoAbbonamento}
Periodo: dal {periodo}

Stato: REGOLARE (PAGATO)
--------------------------------------------------";

        await Shell.Current.DisplayAlert($"🖨️ Ricevuta N° {numRicevuta}", ricevutaTesto, "Stampa / Chiudi");
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

            // Salva gli abbonamenti correlati
            foreach (var abb in ListaAbbonamenti)
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