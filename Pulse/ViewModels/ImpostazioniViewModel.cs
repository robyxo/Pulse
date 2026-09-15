using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pulse.Models;
using Pulse.Services;

namespace Pulse.ViewModels;

public partial class ImpostazioniViewModel : BaseViewModel
{
    private readonly IImpostazioniService _impostazioniService;

    [ObservableProperty]
    private Impostazioni _impostazioni = new();

    // --- VISTA CALENDARIO ---
    [ObservableProperty]
    private bool _orarioScaglionatoAttivo;

    // --- RICEVUTE ---
    [ObservableProperty]
    private bool _stampaRicevutaCortesiaAttiva;

    // --- DOCUMENTO PRIVACY ---
    [ObservableProperty]
    private bool _stampaDocumentoPrivacyAttiva;

    // --- COLORI CORSI ---
    [ObservableProperty]
    private int _numeroColoriCorsi = 6;

    public List<int> OpzioniNumeroColori { get; } = Enumerable.Range(1, 20).ToList();

    public ImpostazioniViewModel(INavigationService navigationService, IImpostazioniService impostazioniService)
        : base(navigationService)
    {
        _impostazioniService = impostazioniService;
        Title = "Impostazioni";
    }

    [RelayCommand]
    public async Task CaricaImpostazioniAsync()
    {
        await EseguiConCaricamento(async () =>
        {
            Impostazioni = await _impostazioniService.GetImpostazioniAsync();
            OrarioScaglionatoAttivo = Impostazioni.OrarioScaglionato == 1;
            StampaRicevutaCortesiaAttiva = Impostazioni.StampaRicevutaCortesia == 1;
            StampaDocumentoPrivacyAttiva = Impostazioni.StampaDocumentoPrivacy == 1;
            NumeroColoriCorsi = Impostazioni.NumeroColoriCorsi ?? 6;
        });
    }

    [RelayCommand]
    public async Task SalvaImpostazioniAsync()
    {
        await EseguiConCaricamento(async () =>
        {
            Impostazioni.OrarioScaglionato = OrarioScaglionatoAttivo ? 1 : 0;
            Impostazioni.StampaRicevutaCortesia = StampaRicevutaCortesiaAttiva ? 1 : 0;
            Impostazioni.StampaDocumentoPrivacy = StampaDocumentoPrivacyAttiva ? 1 : 0;
            Impostazioni.NumeroColoriCorsi = NumeroColoriCorsi;

            await _impostazioniService.SalvaImpostazioniAsync(Impostazioni);
            await Shell.Current.DisplayAlert("Fatto", "Impostazioni salvate correttamente.", "OK");
        });
    }
}