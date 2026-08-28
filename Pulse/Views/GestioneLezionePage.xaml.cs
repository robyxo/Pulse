using Pulse.ViewModels;

namespace Pulse.Views;

public partial class GestioneLezionePage : ContentPage
{
    public GestioneLezionePage(GestioneLezioneViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}