using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pulse.DTO;
using Pulse.Models;
using Pulse.Services;
using Pulse.Utils;
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

    // L'anno del riepilogo e' l'anno scolastico (settembre-agosto) che contiene
    // il mese scelto, come nelle Statistiche.
    [ObservableProperty]
    private string _etichettaAnno = "Anno scolastico";

    // --- PAGAMENTI ---
    private List<PagamentiInsegnanti> _pagamenti = new();

    [ObservableProperty]
    private ObservableCollection<PagamentoMaestroDTO> _listaPagamenti = new();

    [ObservableProperty]
    private bool _nessunPagamento = true;

    [ObservableProperty]
    private double _pagatoMese;

    [ObservableProperty]
    private double _daPagareMese;

    [ObservableProperty]
    private string _etichettaMese = string.Empty;

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

            await CaricaPagamentiInternoAsync();
            await RicalcolaRiepilogoAsync();
        });
    }

    private async Task RicalcolaRiepilogoAsync()
    {
        if (MeseSelezionatoIndex < 0 || MeseSelezionatoIndex > 11) return;

        int mese = MeseSelezionatoIndex + 1;

        OreSettimanali = _compensiService.CalcolaOreSettimanali(_lezioniAssegnate);
        OreMese = await _compensiService.CalcolaOreMeseAsync(_lezioniAssegnate, AnnoSelezionato, mese);
        OreAnno = await _compensiService.CalcolaOreAnnoScolasticoAsync(_lezioniAssegnate, AnnoSelezionato, mese);

        var (dal, _) = CompensiMaestriService.AnnoScolastico(AnnoSelezionato, mese);
        EtichettaAnno = $"A.S. {dal.Year}/{(dal.Year + 1) % 100:00}";
        EtichettaMese = $"{NomiMesi[MeseSelezionatoIndex]} {AnnoSelezionato}";

        RicalcolaCompensi();
    }

    private void RicalcolaCompensi()
    {
        CompensoSettimanale = OreSettimanali * TariffaOraria;
        CompensoMese = OreMese * TariffaOraria;
        CompensoAnno = OreAnno * TariffaOraria;
        RicalcolaPagatoMese();
    }

    // ================================================
    // PAGAMENTI AL MAESTRO
    // ================================================

    private async Task CaricaPagamentiInternoAsync()
    {
        _pagamenti = await _dbService.GetPagamentiInsegnanteAsync(Insegnante.Id);
        ListaPagamenti = new ObservableCollection<PagamentoMaestroDTO>(_pagamenti.Select(p => new PagamentoMaestroDTO(p)));
        NessunPagamento = _pagamenti.Count == 0;
        RicalcolaPagatoMese();
    }

    // Pagato per il mese scelto: conta il mese a cui si riferisce il pagamento,
    // non il giorno in cui e' stato fatto (lo stipendio di settembre pagato il 5 ottobre).
    private void RicalcolaPagatoMese()
    {
        int mese = MeseSelezionatoIndex + 1;

        PagatoMese = _pagamenti
            .Where(p => (p.PeriodoDal ?? p.DataPagamento).Year == AnnoSelezionato
                     && (p.PeriodoDal ?? p.DataPagamento).Month == mese)
            .Sum(p => p.Importo);

        DaPagareMese = Math.Max(0, CompensoMese - PagatoMese);
    }

    [RelayCommand]
    public async Task RegistraPagamentoMeseAsync()
    {
        if (Insegnante == null || Insegnante.Id == 0) return;
        if (MeseSelezionatoIndex < 0 || MeseSelezionatoIndex > 11) return;

        int mese = MeseSelezionatoIndex + 1;
        var inizioMese = new DateTime(AnnoSelezionato, mese, 1);
        var fineMese = inizioMese.AddMonths(1).AddDays(-1);

        string proposta = DaPagareMese > 0 ? DaPagareMese.ToString("0.##", CultureInfo.GetCultureInfo("it-IT")) : string.Empty;

        string? risposta = await AlertPopup.ShowPrompt(
            "Registra pagamento",
            $"Pagamento a {Insegnante.NomeCompleto} per {EtichettaMese}.\n" +
            $"Ore del mese: {OreMese:0.#} — compenso previsto € {CompensoMese:N2}, già pagato € {PagatoMese:N2}.\n\n" +
            "Importo pagato (€):",
            "Registra",
            "Annulla",
            placeholder: "0,00",
            keyboard: Keyboard.Numeric,
            initialValue: proposta);

        if (risposta == null) return; // Annulla

        if (!ProvaLeggiImporto(risposta, out double importo) || importo <= 0)
        {
            await AlertPopup.ShowWarning("Inserisci un importo valido, maggiore di zero.");
            return;
        }

        await EseguiConCaricamento(async () =>
        {
            await _dbService.SalvaPagamentoInsegnanteAsync(new PagamentiInsegnanti
            {
                InsegnanteId = Insegnante.Id,
                DataPagamento = DateTime.Now,
                PeriodoDal = inizioMese,
                PeriodoAl = fineMese,
                OreTotali = OreMese,
                Importo = importo
            });

            await CaricaPagamentiInternoAsync();
        });
    }

    // Accetta "150", "150,50", "1.200,50" e anche "150.50" (punto come decimale).
    private static bool ProvaLeggiImporto(string testo, out double importo)
    {
        testo = testo.Replace("€", string.Empty).Trim();

        int punto = testo.IndexOf('.');
        if (!testo.Contains(',') && punto >= 0 && testo.IndexOf('.', punto + 1) < 0 && testo.Length - punto <= 3)
            testo = testo.Replace('.', ',');

        return double.TryParse(testo, NumberStyles.Number, CultureInfo.GetCultureInfo("it-IT"), out importo);
    }

    [RelayCommand]
    public async Task EliminaPagamentoAsync(PagamentoMaestroDTO riga)
    {
        if (riga == null) return;

        bool conferma = await AlertPopup.ShowConfirmation(
            "Elimina pagamento",
            $"Vuoi eliminare il pagamento di {riga.ImportoTesto} del {riga.DataTesto} ({riga.PeriodoTesto})?",
            "Sì, Elimina",
            "Annulla");

        if (!conferma) return;

        await EseguiConCaricamento(async () =>
        {
            await _dbService.EliminaPagamentoInsegnanteAsync(riga.Pagamento.Id);
            await CaricaPagamentiInternoAsync();
        });
    }

    [RelayCommand]
    public async Task SalvaInsegnanteAsync()
    {
        if (string.IsNullOrWhiteSpace(Nome) || string.IsNullOrWhiteSpace(Cognome))
        {
            await AlertPopup.Show("Attenzione", "Inserisci sia il nome che il cognome.", "OK");
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

        bool conferma = await AlertPopup.ShowConfirmation(
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