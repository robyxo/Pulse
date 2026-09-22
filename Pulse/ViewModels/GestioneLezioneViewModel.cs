using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Pulse.DTO;
using Pulse.Helpers;
using Pulse.Models;
using Pulse.Services;
using System.Collections.ObjectModel;

namespace Pulse.ViewModels;

[QueryProperty(nameof(Lezione), "Lezione")]
public partial class GestioneLezioneViewModel : BaseViewModel
{
    private readonly IDatabaseService _dbService;
    private readonly RicevutaService _ricevutaService;

    private readonly IImpostazioniService _impostazioniService;

    private bool _stampaRicevutaCortesiaAttiva = true;

    [ObservableProperty]
    private Lezioni _lezione = new();

    [ObservableProperty]
    private Corsi? _corsoSelezionato;

    [ObservableProperty]
    private Insegnanti? _maestroSelezionato;

    [ObservableProperty]
    private Sale? _salaSelezionata;

    [ObservableProperty]
    private string _giornoSelezionato = "Lunedì";

    [ObservableProperty]
    private TimeSpan _oraInizio = new(19, 0, 0);

    [ObservableProperty]
    private TimeSpan _oraFine = new(20, 0, 0);

    [ObservableProperty]
    private bool _isEdizione = false;

    [ObservableProperty]
    private string _titoloAllievi = "👥 Allievi Iscritti (0)";

    [ObservableProperty]
    private bool _nessunAllievoPresente = true;

    [ObservableProperty]
    private ObservableCollection<Corsi> _listaCorsi = new();

    [ObservableProperty]
    private ObservableCollection<Insegnanti> _listaMaestri = new();

    [ObservableProperty]
    private ObservableCollection<Sale> _listaSale = new();

    [ObservableProperty]
    private ObservableCollection<AllievoPresenzaDTO> _listaAllievi = new();

    public List<string> GiorniSettimana { get; } = DateHelper.GiorniSettimana.ToList();

    public GestioneLezioneViewModel(IDatabaseService dbService, IImpostazioniService impostazioniService, RicevutaService ricevutaService)
    {
        _dbService = dbService;
        _impostazioniService = impostazioniService;
        _ricevutaService = ricevutaService;
        Title = "Gestione Lezione";

        _ = CaricaFlagImpostazioniAsync();
    }

    private async Task CaricaFlagImpostazioniAsync()
    {
        var impostazioni = await _impostazioniService.GetImpostazioniAsync();
        _stampaRicevutaCortesiaAttiva = impostazioni.StampaRicevutaCortesia == 1;
    }

    partial void OnLezioneChanged(Lezioni value)
    {
        if (value == null) return;
        _ = InizializzaDatiAsync();
    }

    partial void OnOraInizioChanged(TimeSpan oldValue, TimeSpan newValue)
    {
        var durata = OraFine - oldValue;
        if (durata <= TimeSpan.Zero)
            durata = TimeSpan.FromHours(1);

        var nuovaFine = newValue.Add(durata);
        OraFine = nuovaFine < TimeSpan.FromDays(1)
            ? nuovaFine
            : new TimeSpan(23, 59, 0);
    }

    private async Task InizializzaDatiAsync()
    {
        await EseguiConCaricamento(async () =>
        {
            var corsi = await _dbService.GetCorsiAttiviAsync();
            var maestri = await _dbService.GetInsegnantiAttiviAsync();
            var sale = await _dbService.GetSaleAttiveAsync();

            ListaCorsi = new ObservableCollection<Corsi>(corsi);
            ListaMaestri = new ObservableCollection<Insegnanti>(maestri);
            ListaSale = new ObservableCollection<Sale>(sale);

            IsEdizione = Lezione.Id > 0;

            int indexGiorno = Math.Clamp(Lezione.GiornoSettimana - 1, 0, 6);
            GiornoSelezionato = GiorniSettimana[indexGiorno];

            if (TimeSpan.TryParse(Lezione.OraInizio, out var tInizio))
                OraInizio = tInizio;

            if (TimeSpan.TryParse(Lezione.OraFine, out var tFine))
                OraFine = tFine;

            if (Lezione.CorsoId > 0)
                CorsoSelezionato = ListaCorsi.FirstOrDefault(c => c.Id == Lezione.CorsoId);

            if (Lezione.InsegnanteId.HasValue && Lezione.InsegnanteId.Value > 0)
                MaestroSelezionato = ListaMaestri.FirstOrDefault(m => m.Id == Lezione.InsegnanteId.Value);

            if (Lezione.SalaId.HasValue && Lezione.SalaId.Value > 0)
                SalaSelezionata = ListaSale.FirstOrDefault(s => s.Id == Lezione.SalaId.Value);
            else if (ListaSale.Count == 1)
                SalaSelezionata = ListaSale[0]; // Con una sola sala non ha senso farla scegliere

            await CaricaAllieviPerCorsoAsync();
        });
    }

    partial void OnCorsoSelezionatoChanged(Corsi? value)
    {
        _ = CaricaAllieviPerCorsoAsync();
    }

    private async Task CaricaAllieviPerCorsoAsync()
    {
        if (CorsoSelezionato == null)
        {
            ListaAllievi.Clear();
            NessunAllievoPresente = true;
            TitoloAllievi = "👥 Allievi Iscritti (0)";
            return;
        }

        // Due query invece di una per ogni allievo iscritto
        var allievi = await _dbService.GetAllieviPerCorsoAsync(CorsoSelezionato.Id);
        var abbonamenti = await _dbService.GetAbbonamentiAttiviPerCorsoAsync(CorsoSelezionato.Id);

        var abbonamentoPerAllievo = abbonamenti
            .GroupBy(a => a.AllievoId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.DataInizio).First());

        var dtos = allievi
            .Select(allievo => new AllievoPresenzaDTO
            {
                Allievo = allievo,
                Abbonamento = abbonamentoPerAllievo.TryGetValue(allievo.Id, out var abb) ? abb : null
            })
            .ToList();

        ListaAllievi = new ObservableCollection<AllievoPresenzaDTO>(dtos);
        NessunAllievoPresente = ListaAllievi.Count == 0;
        TitoloAllievi = $"👥 Allievi Iscritti ({ListaAllievi.Count})";
    }

    [RelayCommand]
    public async Task PagamentoRapidoAsync(AllievoPresenzaDTO item)
    {
        if (item?.Allievo == null || CorsoSelezionato == null) return;

        string opzioneScelta = await Shell.Current.DisplayActionSheet(
            $"Incasso per {item.NomeCompleto}:",
            "Annulla",
            null,
            $"Singolo / Giornata (€ {CorsoSelezionato.CostoSingolo ?? 0:N2})",
            $"Mensile 4 Settimane (€ {CorsoSelezionato.CostoMensile ?? 0:N2})");

        if (string.IsNullOrEmpty(opzioneScelta) || opzioneScelta == "Annulla") return;

        bool isSingolo = opzioneScelta.StartsWith("Singolo");
        string tipoAbb = isSingolo ? "Singolo" : "Mensile";
        double importo = isSingolo ? (CorsoSelezionato.CostoSingolo ?? 0) : (CorsoSelezionato.CostoMensile ?? 0);

        DateTime dataInizio = DateTime.Now;
        DateTime dataFine;

        if (isSingolo)
        {
            dataFine = DateTime.Now.Date.AddDays(1).AddTicks(-1);
        }
        else
        {
            DateTime dataBase = (item.Abbonamento != null && item.Abbonamento.DataScadenza >= DateTime.Today)
                ? item.Abbonamento.DataScadenza
                : DateTime.Today;
            dataFine = dataBase.AddDays(28);
        }

        var nuovoAbbonamento = new Abbonamenti
        {
            AllievoId = item.Allievo.Id,
            Allievo = item.Allievo,
            CorsoId = CorsoSelezionato.Id,
            Corso = CorsoSelezionato,
            TipoAbbonamento = tipoAbb,
            DataInizio = dataInizio,
            DataScadenza = dataFine,
            ImportoTotale = importo,
            ImportoPagato = importo,
            IsPagato = 1,
            IsSospeso = 0,
            Attivo = 1
        };

        await EseguiConCaricamento(async () =>
        {
            await _dbService.SalvaAbbonamentoAsync(nuovoAbbonamento);
            await CaricaAllieviPerCorsoAsync();
        });

        if (_stampaRicevutaCortesiaAttiva)
        {
            bool stampa = await Shell.Current.DisplayAlert(
                "Pagamento Registrato",
                $"Incasso di € {importo:N2} salvato con successo.\nNuova scadenza: {dataFine:dd/MM/yyyy}.\n\nVuoi stampare la ricevuta di cortesia?",
                "Sì, Stampa",
                "No");

            if (stampa)
            {
                await _ricevutaService.StampaRicevutaCortesiaAsync(nuovoAbbonamento);
            }
        }
    }

    [RelayCommand]
    public async Task SalvaLezioneAsync()
    {
        if (CorsoSelezionato == null || MaestroSelezionato == null)
        {
            await Shell.Current.DisplayAlert("Attenzione", "Seleziona sia un corso che un maestro.", "OK");
            return;
        }

        if (OraInizio >= OraFine)
        {
            await Shell.Current.DisplayAlert("Attenzione", "L'orario di inizio deve precedere quello di fine.", "OK");
            return;
        }

        int giornoDb = GiorniSettimana.IndexOf(GiornoSelezionato) + 1;

        // La sovrapposizione in sala e' solo una segnalazione: la scuola puo'
        // avere motivi validi per accavallare due lezioni, quindi si avvisa
        // e si lascia decidere.
        string? conflitto = await CercaConflittoSalaAsync(giornoDb);
        if (conflitto != null)
        {
            string inizioTesto = OraInizio.ToString(@"hh\:mm");
            string fineTesto = OraFine.ToString(@"hh\:mm");

            bool prosegui = await Shell.Current.DisplayAlert(
                "⚠️ Sala già occupata",
                $"In «{SalaSelezionata!.Nome}», {GiornoSelezionato.ToLower()} dalle {inizioTesto} alle {fineTesto}, c'è già:\n\n{conflitto}\n\nVuoi salvare lo stesso?",
                "Salva comunque",
                "Annulla");

            if (!prosegui) return;
        }

        await EseguiConCaricamento(async () =>
        {
            Lezione.CorsoId = CorsoSelezionato.Id;
            Lezione.InsegnanteId = MaestroSelezionato.Id;
            Lezione.SalaId = SalaSelezionata?.Id;
            Lezione.GiornoSettimana = giornoDb;
            Lezione.OraInizio = OraInizio.ToString(@"hh\:mm");
            Lezione.OraFine = OraFine.ToString(@"hh\:mm");

            await _dbService.SalvaLezioneAsync(Lezione);
            WeakReferenceMessenger.Default.Send(new CalendarioViewModel.RefreshGridMessage());
            await Shell.Current.Navigation.PopAsync();
        });
    }

    /// <summary>
    /// Cerca lezioni gia' presenti nella stessa sala, nello stesso giorno, con
    /// orari che si accavallano. Restituisce l'elenco pronto da mostrare,
    /// oppure null se non c'e' nessuna sovrapposizione.
    /// </summary>
    private async Task<string?> CercaConflittoSalaAsync(int giornoDb)
    {
        if (SalaSelezionata == null) return null;

        var altreLezioni = await _dbService.GetLezioniPerSalaEGiornoAsync(
            SalaSelezionata.Id,
            giornoDb,
            Lezione.Id);

        // Due intervalli si sovrappongono se ciascuno inizia prima che l'altro finisca.
        var sovrapposte = altreLezioni
            .Where(l => TimeSpan.TryParse(l.OraInizio, out var inizio)
                     && TimeSpan.TryParse(l.OraFine, out var fine)
                     && inizio < OraFine
                     && fine > OraInizio)
            .ToList();

        if (sovrapposte.Count == 0) return null;

        var righe = sovrapposte.Select(l =>
        {
            string maestro = l.Insegnante != null
                ? $" — {l.Insegnante.Nome} {l.Insegnante.Cognome}".TrimEnd()
                : string.Empty;

            return $"• {l.Corso?.Nome ?? "Lezione"}  {l.OraInizio}-{l.OraFine}{maestro}";
        });

        return string.Join("\n", righe);
    }

    [RelayCommand]
    public async Task EliminaLezioneAsync()
    {
        if (Lezione.Id == 0) return;

        bool conferma = await Shell.Current.DisplayAlert(
            "Elimina Lezione",
            "Vuoi eliminare questa lezione dal calendario?",
            "Sì, Elimina",
            "Annulla");

        if (conferma)
        {
            await EseguiConCaricamento(async () =>
            {
                await _dbService.EliminaLezioneAsync(Lezione.Id);
                WeakReferenceMessenger.Default.Send(new CalendarioViewModel.RefreshGridMessage());
                await Shell.Current.Navigation.PopAsync();
            });
        }
    }

    [RelayCommand]
    public async Task SaltaLezioneAsync()
    {
        if (CorsoSelezionato == null || ListaAllievi.Count == 0)
        {
            await Shell.Current.DisplayAlert("Attenzione", "Nessun allievo con abbonamento attivo da prorogare per questo corso.", "OK");
            return;
        }

        bool conferma = await Shell.Current.DisplayAlert(
            "Salta Lezione",
            $"⚠️ Attenzione: confermando, a tutti gli allievi con abbonamento attivo su '{CorsoSelezionato.Nome}' verrà aggiunta automaticamente 1 settimana di validità, per recuperare la lezione saltata.\n\nProcedere?",
            "Sì, Salta Lezione",
            "Annulla");

        if (!conferma) return;

        await EseguiConCaricamento(async () =>
        {
            foreach (var item in ListaAllievi)
            {
                if (item.Abbonamento == null) continue;

                item.Abbonamento.DataScadenza = item.Abbonamento.DataScadenza.AddDays(7);
                await _dbService.SalvaAbbonamentoAsync(item.Abbonamento);
            }

            await CaricaAllieviPerCorsoAsync();
        });

        await Shell.Current.DisplayAlert("Fatto", "Lezione saltata: gli abbonamenti degli allievi sono stati prorogati di 1 settimana.", "OK");
    }

}