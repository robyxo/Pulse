using Pulse.ViewModels;

namespace Pulse.Views;

public partial class SalePage : ContentPage
{
    private readonly SaleViewModel _viewModel;

    public SalePage(SaleViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _viewModel.CaricaSaleAsync();
    }
}
