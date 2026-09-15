using Pulse.ViewModels;

namespace Pulse.Views;

public partial class GestioneCorsoPage : ContentPage
{
    public GestioneCorsoPage(GestioneCorsoViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
    private void ColorePreset_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button &&
            button.BindingContext is Pulse.DTO.OpzioneColore opzione &&
            BindingContext is GestioneCorsoViewModel viewModel)
        {
            viewModel.SelezionaColorePresetCommand.Execute(opzione.Hex);
        }
    }
}