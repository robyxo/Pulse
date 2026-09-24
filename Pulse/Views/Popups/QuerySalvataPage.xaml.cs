using Pulse.Models;

namespace Pulse.Views.Popups;

/// <summary>
/// Editor di una query salvata, aperto a tutto schermo dalla pagina Statistiche.
/// Non salva da solo: passa la query a chi l'ha aperto, come NuovoAbbonamentoPage.
/// </summary>
public partial class QuerySalvataPage : ContentPage
{
    private readonly QuerySalvate _query;
    private readonly Func<QuerySalvate, Task> _onSalva;
    private readonly Func<string, Task<(bool Riuscita, string Messaggio)>> _onProva;

    public QuerySalvataPage(
        QuerySalvate query,
        bool mostraSoloAmministratore,
        Func<QuerySalvate, Task> onSalva,
        Func<string, Task<(bool Riuscita, string Messaggio)>> onProva)
    {
        InitializeComponent();
        _query = query;
        _onSalva = onSalva;
        _onProva = onProva;

        LblTitolo.Text = query.Id > 0 ? "🔎 Modifica query" : "🔎 Nuova query";
        EntryNome.Text = query.Nome;
        EntryDescrizione.Text = query.Descrizione;
        EntryCategoria.Text = query.Categoria;
        EditorQuery.Text = query.TestoQuery;
        SwitchSoloAmministratore.IsToggled = query.SoloAmministratore == 1;

        // Fuori da Visual Studio l'opzione non si mostra: una scuola che la
        // attivasse vedrebbe sparire la propria query.
        RigaSoloAmministratore.IsVisible = mostraSoloAmministratore;
    }

    private async void OnProvaClicked(object sender, EventArgs e)
    {
        LblEsitoProva.IsVisible = true;
        LblEsitoProva.TextColor = Color.FromArgb("#64748B");
        LblEsitoProva.Text = "Esecuzione in corso...";

        var (riuscita, messaggio) = await _onProva(EditorQuery.Text ?? string.Empty);

        LblEsitoProva.TextColor = Color.FromArgb(riuscita ? "#166534" : "#B91C1C");
        LblEsitoProva.Text = messaggio;
    }

    private async void OnAnnullaClicked(object sender, EventArgs e)
        => await Navigation.PopModalAsync();

    private async void OnSalvaClicked(object sender, EventArgs e)
    {
        string nome = EntryNome.Text?.Trim() ?? string.Empty;
        string testo = EditorQuery.Text?.Trim() ?? string.Empty;

        if (nome.Length == 0)
        {
            await DisplayAlert("Attenzione", "Dai un nome alla query.", "OK");
            return;
        }

        if (testo.Length == 0)
        {
            await DisplayAlert("Attenzione", "Scrivi il testo della query.", "OK");
            return;
        }

        _query.Nome = nome;
        _query.Descrizione = string.IsNullOrWhiteSpace(EntryDescrizione.Text) ? null : EntryDescrizione.Text.Trim();
        _query.Categoria = string.IsNullOrWhiteSpace(EntryCategoria.Text) ? null : EntryCategoria.Text.Trim();
        _query.TestoQuery = testo;
        _query.SoloAmministratore = SwitchSoloAmministratore.IsToggled ? 1 : 0;

        await _onSalva(_query);
        await Navigation.PopModalAsync();
    }
}
