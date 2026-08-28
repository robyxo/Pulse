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
            // 1. Carica Corsi e Insegnanti attivi dal Database
            var corsi = await _dbService.GetCorsiAttiviAsync();
            var maestri = await _dbService.GetInsegnantiAttiviAsync();

            ListaCorsi = new ObservableCollection<Corsi>(corsi);
            ListaMaestri = new ObservableCollection<Insegnanti>(maestri);

            IsEdizione = Lezione.Id > 0;

            // 2. Imposta giorno
            int indexGiorno = Math.Clamp(Lezione.GiornoSettimana - 1, 0, 6);
            GiornoSelezionato = GiorniSettimana[indexGiorno];

            // 3. Imposta orari
            if (TimeSpan.TryParse(Lezione.OraInizio, out var tInizio))
                OraInizio = tInizio;

            if (TimeSpan.TryParse(Lezione.OraFine, out var tFine))
                OraFine = tFine;

            // 4. Preseleziona Corso se presente
            if (Lezione.CorsoId > 0)
                CorsoSelezionato = ListaCorsi.FirstOrDefault(c => c.Id == Lezione.CorsoId);

            // 5. Preseleziona Maestro se presente
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