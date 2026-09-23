using Pulse.ViewModels;

namespace Pulse.Views;

public partial class NotifichePage : ContentPage
{
    private readonly NotificheViewModel _viewModel;

    public NotifichePage(NotificheViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _viewModel.CaricaComunicazioniAsync();
    }
}
