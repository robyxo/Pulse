namespace Pulse.Views;

public partial class CorsiPage : ContentPage
{
    private readonly CorsiViewModel _viewModel;

    public CorsiPage(CorsiViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _viewModel.CaricaCorsiAsync();
    }
}