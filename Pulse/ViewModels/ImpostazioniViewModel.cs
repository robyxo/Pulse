using Pulse.Services;

namespace Pulse.ViewModels;

public partial class ImpostazioniViewModel : BaseViewModel
{
    public ImpostazioniViewModel(INavigationService navigationService) : base(navigationService)
    {
        Title = "Impostazioni";
    }
}