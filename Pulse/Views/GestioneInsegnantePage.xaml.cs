namespace Pulse.Views;

public partial class GestioneInsegnantePage : ContentPage
{
    public GestioneInsegnantePage(GestioneInsegnanteViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}