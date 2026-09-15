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
        });
    }

    [RelayCommand]
    public async Task SalvaImpostazioniAsync()
    {
        await EseguiConCaricamento(async () =>
        {
            Impostazioni.OrarioScaglionato = OrarioScaglionatoAttivo ? 1 : 0;

            await _impostazioniService.SalvaImpostazioniAsync(Impostazioni);
            await Shell.Current.DisplayAlert("Fatto", "Impostazioni salvate correttamente.", "OK");
        });
    }
}