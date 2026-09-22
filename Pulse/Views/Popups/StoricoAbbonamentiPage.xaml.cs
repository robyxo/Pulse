using Pulse.Helpers;
using Pulse.Models;

namespace Pulse.Views.Popups;

/// <summary>
/// Riga di sola visualizzazione usata dalla lista dello storico.
/// Tipo pubblico e di primo livello cosi' il DataTemplate puo' dichiarare x:DataType.
/// </summary>
public class RigaStorico
{
    public string Periodo { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Importo { get; set; } = string.Empty;
    public string Stato { get; set; } = string.Empty;
    public Color ColoreStato { get; set; } = Colors.Gray;
}

public partial class StoricoAbbonamentiPage : ContentPage
{
    public StoricoAbbonamentiPage(string nomeCorso, string nomeAllievo, List<Abbonamenti> abbonamenti)
    {
        InitializeComponent();

        abbonamenti ??= new List<Abbonamenti>();

        LblCorso.Text = $"Corso: {nomeCorso}";
        LblAllievo.Text = nomeAllievo;
        LblConteggio.Text = abbonamenti.Count == 1
            ? "1 abbonamento registrato"
            : $"{abbonamenti.Count} abbonamenti registrati";

        var righe = abbonamenti
            .OrderByDescending(a => a.DataInizio)
            .ThenByDescending(a => a.Id)
            .Select(a =>
            {
                var stato = StatoAbbonamentoHelper.Calcola(a);
                return new RigaStorico
                {
                    Periodo = $"{a.DataInizio:dd/MM/yyyy} → {a.DataScadenza:dd/MM/yyyy}",
                    Tipo = a.Id == 0
                        ? $"{a.TipoAbbonamento} · non ancora salvato"
                        : a.TipoAbbonamento,
                    Importo = $"€ {a.ImportoPagato:N2}",
                    Stato = StatoAbbonamentoHelper.GetEtichettaConIcona(stato),
                    ColoreStato = Color.FromArgb(StatoAbbonamentoHelper.GetColore(stato))
                };
            })
            .ToList();

        ListaStorico.ItemsSource = righe;

        double totale = abbonamenti.Sum(a => a.ImportoPagato);
        LblTotale.Text = $"Totale incassato: € {totale:N2}";
    }

    private async void OnChiudiClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}
