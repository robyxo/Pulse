using Pulse.ViewModels;

namespace Pulse.Views;

public partial class CalendarioEventiPage : ContentPage
{
    private readonly CalendarioEventiViewModel _viewModel;

    public CalendarioEventiPage(CalendarioEventiViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _viewModel.CaricaDatiAsync();
    }
}