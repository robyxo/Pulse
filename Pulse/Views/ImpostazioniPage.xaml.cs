namespace Pulse.Views;

public partial class ImpostazioniPage : ContentPage
{
    public ImpostazioniPage(ImpostazioniViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}