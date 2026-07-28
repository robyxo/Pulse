using Pulse.Models;

namespace Pulse.Views.Popups;

public partial class AllieviCorsoPage : ContentPage
{
    private Lezioni _lezione;
    public AllieviCorsoPage(Lezioni lezione)
    {
        InitializeComponent();
        _lezione = lezione;
        LblTitolo.Text = lezione.Corso?.Nome;
        LblOrario.Text = $"{lezione.OraInizio} - {lezione.OraFine}";
        LblMaestro.Text = $"Maestro: {lezione.Insegnante?.Nome}";
        ListaAllievi.ItemsSource = lezione.Corso?.Iscrizionis;
    }

    private async void OnChiudiClicked(object sender, EventArgs e)
        => await Navigation.PopAsync();
}