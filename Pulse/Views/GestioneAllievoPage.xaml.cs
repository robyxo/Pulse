using Pulse.ViewModels;

namespace Pulse.Views;

public partial class GestioneAllievoPage : ContentPage
{
    public GestioneAllievoPage(GestioneAllievoViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}