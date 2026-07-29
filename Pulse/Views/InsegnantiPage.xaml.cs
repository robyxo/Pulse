namespace Pulse.Views;

public partial class InsegnantiPage : ContentPage
{
    private readonly InsegnantiViewModel _viewModel;

    public InsegnantiPage(InsegnantiViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.CaricaInsegnantiAsync();
    }
}