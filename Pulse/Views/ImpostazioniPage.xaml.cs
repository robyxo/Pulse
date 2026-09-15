namespace Pulse.Views;

public partial class ImpostazioniPage : ContentPage
{
    private readonly ImpostazioniViewModel _viewModel;

    public ImpostazioniPage(ImpostazioniViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _viewModel.CaricaImpostazioniAsync();
    }
}