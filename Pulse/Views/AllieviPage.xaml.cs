using Pulse.ViewModels;

namespace Pulse.Views;

public partial class AllieviPage : ContentPage
{
    private readonly AllieviViewModel _viewModel;

    public AllieviPage(AllieviViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.CaricaAllieviAsync();
    }
}