using Pulse.ViewModels;

namespace Pulse.Views;

public partial class GestioneCorsoPage : ContentPage
{
    public GestioneCorsoPage(GestioneCorsoViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}