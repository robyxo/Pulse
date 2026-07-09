using Pulse.Models;

namespace Pulse.Views.Popups;

public partial class AllieviCorsoPage : ContentPage
{
    public AllieviCorsoPage(string nomeCorso, List<Allievi> allievi)
    {
        InitializeComponent();
        LblTitolo.Text = nomeCorso;
        ListaAllievi.ItemsSource = allievi;
    }

    private async void OnChiudiClicked(object sender, EventArgs e)
        => await Navigation.PopAsync();
}