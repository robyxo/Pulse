namespace Pulse.Views;

public partial class StatistichePage : ContentPage
{
    private readonly StatisticheViewModel _viewModel;

    public StatistichePage(StatisticheViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.CaricaAsync();
    }
}
