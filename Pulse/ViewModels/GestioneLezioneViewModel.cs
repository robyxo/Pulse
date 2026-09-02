using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Pulse.DTO;
using Pulse.Models;
using Pulse.Services;
using System.Collections.ObjectModel;

namespace Pulse.ViewModels;

[QueryProperty(nameof(Lezione), "Lezione")]
public partial class GestioneLezioneViewModel : BaseViewModel
{
    private readonly IDatabaseService _dbService;

    [ObservableProperty]
    private Lezioni _lezione = new();

    [ObservableProperty]
    private Corsi? _corsoSelezionato;

    [ObservableProperty]
    private Insegnanti? _maestroSelezionato;

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
    private ObservableCollection<AllievoPresenzaDTO> _listaAllievi = new();

    public List<string> GiorniSettimana { get; } = new()
    {
        "Lunedì", "Martedì", "Mercoledì", "Giovedì", "Venerdì", "Sabato", "Domenica"
    };

    public GestioneLezioneViewModel(INavigationService navigationService, IDatabaseService dbService)
        : base(navigationService)
    {
        _dbService = dbService;
        Title = "Gestione Lezione";
    }

    partial void OnLezioneChanged(Lezioni value)
    {
        if (value == null) return;
        _ = InizializzaDatiAsync();
    }

    private async Task InizializzaDatiAsync()
    {
        await EseguiConCaricamento(async () =>
        {
            var corsi = await _dbService.GetCorsiAttiviAsync();
            var maestri = await _dbService.GetInsegnantiAttiviAsync();

            ListaCorsi = new ObservableCollection<Corsi>(corsi);
            ListaMaestri = new ObservableCollection<Insegnanti>(maestri);

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

        var allievi = await _dbService.GetAllieviPerCorsoAsync(CorsoSelezionato.Id);
        var dtos = new List<AllievoPresenzaDTO>();

        foreach (var allievo in allievi)
        {
            var abb = (await _dbService.GetAbbonamentiAllievoAsync(allievo.Id))
                      .FirstOrDefault(a => a.CorsoId == CorsoSelezionato.Id && a.Attivo == 1);

            dtos.Add(new AllievoPresenzaDTO
            {
                Allievo = allievo,
                Abbonamento = abb
            });
        }

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

        bool stampa = await Shell.Current.DisplayAlert(
            "Pagamento Registrato",
            $"Incasso di € {importo:N2} salvato con successo.\nNuova scadenza: {dataFine:dd/MM/yyyy}.\n\nVuoi stampare la ricevuta di cortesia?",
            "Sì, Stampa",
            "No");

        if (stampa)
        {
            string nomeScuola = Preferences.Get("Scuola_Nome", "ASD SCUOLA DI DANZA PULSE");
            string indirizzoScuola = Preferences.Get("Scuola_Indirizzo", "Via Roma 123 - San Benedetto del Tronto (AP)");
            string pivaScuola = Preferences.Get("Scuola_PIVA", "01234567890");

            var ricevutaService = new RicevutaService();
            await ricevutaService.StampaRicevutaCortesiaAsync(nuovoAbbonamento, nomeScuola, indirizzoScuola, pivaScuola);
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

        await EseguiConCaricamento(async () =>
        {
            Lezione.CorsoId = CorsoSelezionato.Id;
            Lezione.InsegnanteId = MaestroSelezionato.Id;
            Lezione.GiornoSettimana = GiorniSettimana.IndexOf(GiornoSelezionato) + 1;
            Lezione.OraInizio = OraInizio.ToString(@"hh\:mm");
            Lezione.OraFine = OraFine.ToString(@"hh\:mm");

            await _dbService.SalvaLezioneAsync(Lezione);
            WeakReferenceMessenger.Default.Send(new CalendarioViewModel.RefreshGridMessage());
            await Shell.Current.Navigation.PopAsync();
        });
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
}