namespace Pulse.Views;

public partial class NotifichePage : ContentPage
{
    public NotifichePage(NotificheViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}