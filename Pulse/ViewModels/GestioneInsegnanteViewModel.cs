using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pulse.DTO;
using Pulse.Models;
using Pulse.Services;
using System.Collections.ObjectModel;
using System.Globalization;

namespace Pulse.ViewModels;

[QueryProperty(nameof(Insegnante), "Insegnante")]
public partial class GestioneInsegnanteViewModel : BaseViewModel
{
    private readonly IDatabaseService _dbService;
    private readonly IImpostazioniService _impostazioniService;
    private readonly CompensiMaestriService _compensiService;

    private List<Lezioni> _lezioniAssegnate = new();

    [ObservableProperty]
    private Insegnanti _insegnante = new();

    [ObservableProperty]
    private string _nome = string.Empty;

    [ObservableProperty]
    private string _cognome = string.Empty;

    [ObservableProperty]
    private string _telefono = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private double _tariffaOraria;

    [ObservableProperty]
    private bool _isEdizione = false;

    // --- FUNZIONE MAESTRO AVANZATA ---
    [ObservableProperty]
    private bool _funzioneMaestroAvanzataAttiva;

    [ObservableProperty]
    private ObservableCollection<LezioneMaestroDTO> _listaLezioniAssegnate = new();

    [ObservableProperty]
    private int _meseSelezionatoIndex = DateTime.Today.Month - 1;

    [ObservableProperty]
    private int _annoSelezionato = DateTime.Today.Year;

    public List<string> NomiMesi { get; } = CultureInfo.GetCultureInfo("it-IT").DateTimeFormat.MonthNames
        .Where(m => !string.IsNullOrEmpty(m)).ToList();

    public List<int> AnniDisponibili { get; } = Enumerable.Range(DateTime.Today.Year - 2, 4).ToList();

    [ObservableProperty]
    private double _oreSettimanali;

    [ObservableProperty]
    private double _compensoSettimanale;

    [ObservableProperty]
    private double _oreMese;

    [ObservableProperty]
    private double _compensoMese;

    [ObservableProperty]
    private double _oreAnno;

    [ObservableProperty]
    private double _compensoAnno;

    public GestioneInsegnanteViewModel(IDatabaseService dbService, IImpostazioniService impostazioniService, CompensiMaestriService compensiService)
    {
        _dbService = dbService;
        _impostazioniService = impostazioniService;
        _compensiService = compensiService;
        Title = "Gestione Insegnante";

        _ = CaricaFlagImpostazioniAsync();
    }

    private async Task CaricaFlagImpostazioniAsync()
    {
        var impostazioni = await _impostazioniService.GetImpostazioniAsync();
        FunzioneMaestroAvanzataAttiva = impostazioni.FunzioneMaestroAvanzataAttiva == 1;
    }

    partial void OnInsegnanteChanged(Insegnanti value)
    {
        if (value == null) return;

        Nome = value.Nome ?? string.Empty;
        Cognome = value.Cognome ?? string.Empty;
        Telefono = value.Telefono ?? string.Empty;
        Email = value.Email ?? string.Empty;
        TariffaOraria = value.TariffaOraria ?? 0;

        IsEdizione = value.Id > 0;

        if (IsEdizione)
        {
            _ = CaricaRiepilogoMaestroAsync();
        }
    }

    partial void OnMeseSelezionatoIndexChanged(int value) => _ = RicalcolaRiepilogoAsync();
    partial void OnAnnoSelezionatoChanged(int value) => _ = RicalcolaRiepilogoAsync();
    partial void OnTariffaOrariaChanged(double value) => RicalcolaCompensi();

    public async Task CaricaRiepilogoMaestroAsync()
    {
        if (Insegnante == null || Insegnante.Id == 0) return;

        await EseguiConCaricamento(async () =>
        {
            _lezioniAssegnate = await _dbService.GetLezioniPerInsegnanteAsync(Insegnante.Id);
            ListaLezioniAssegnate = new ObservableCollection<LezioneMaestroDTO>(
                _lezioniAssegnate.Select(l => new LezioneMaestroDTO(l)));

            await RicalcolaRiepilogoAsync();
        });
    }

    private async Task RicalcolaRiepilogoAsync()
    {
        int mese = MeseSelezionatoIndex + 1;

        OreSettimanali = _compensiService.CalcolaOreSettimanali(_lezioniAssegnate);
        OreMese = await _compensiService.CalcolaOreMeseAsync(_lezioniAssegnate, AnnoSelezionato, mese);
        OreAnno = await _compensiService.CalcolaOreAnnoAsync(_lezioniAssegnate, AnnoSelezionato);

        RicalcolaCompensi();
    }

    private void RicalcolaCompensi()
    {
        CompensoSettimanale = OreSettimanali * TariffaOraria;
        CompensoMese = OreMese * TariffaOraria;
        CompensoAnno = OreAnno * TariffaOraria;
    }

    [RelayCommand]
    public async Task SalvaInsegnanteAsync()
    {
        if (string.IsNullOrWhiteSpace(Nome) || string.IsNullOrWhiteSpace(Cognome))
        {
            await Shell.Current.DisplayAlert("Attenzione", "Inserisci sia il nome che il cognome.", "OK");
            return;
        }

        await EseguiConCaricamento(async () =>
        {
            Insegnante ??= new Insegnanti();

            Insegnante.Nome = Nome.Trim();
            Insegnante.Cognome = Cognome.Trim();
            Insegnante.Telefono = Telefono?.Trim();
            Insegnante.Email = Email?.Trim();
            Insegnante.TariffaOraria = TariffaOraria;

            await _dbService.SalvaInsegnanteAsync(Insegnante);
            await Shell.Current.Navigation.PopAsync();
        });
    }

    [RelayCommand]
    public async Task EliminaInsegnanteAsync()
    {
        if (Insegnante == null || Insegnante.Id == 0) return;

        bool conferma = await Shell.Current.DisplayAlert(
            "Conferma Eliminazione",
            $"Sei sicuro di voler eliminare l'insegnante '{Insegnante.Nome} {Insegnante.Cognome}'?",
            "Sì, Elimina",
            "Annulla");

        if (conferma)
        {
            await EseguiConCaricamento(async () =>
            {
                await _dbService.EliminaInsegnanteAsync(Insegnante.Id);
                await Shell.Current.Navigation.PopAsync();
            });
        }
    }
}