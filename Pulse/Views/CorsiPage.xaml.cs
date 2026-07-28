namespace Pulse.Views;

public partial class CorsiPage : ContentPage
{
    private readonly CorsiViewModel _viewModel;

    public CorsiPage(CorsiViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.CaricaCorsiCommand.ExecuteAsync(null);
    }
}