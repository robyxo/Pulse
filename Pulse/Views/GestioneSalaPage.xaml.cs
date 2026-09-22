using Pulse.ViewModels;

namespace Pulse.Views;

public partial class GestioneSalaPage : ContentPage
{
    public GestioneSalaPage(GestioneSalaViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
