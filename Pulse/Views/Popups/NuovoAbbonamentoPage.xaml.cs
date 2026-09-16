using Pulse.Models;

namespace Pulse.Views.Popups;

public partial class NuovoAbbonamentoPage : ContentPage
{
    private readonly Corsi _corso;
    private readonly Func<Abbonamenti, Task> _onSalva;

    private double _importoStandard;
    private DateTime _scadenzaStandard;
    private string _tipoAbbonamento = "Mensile";

    public NuovoAbbonamentoPage(Corsi corso, Func<Abbonamenti, Task> onSalva)
    {
        InitializeComponent();
        _corso = corso;
        _onSalva = onSalva;

        LblCorso.Text = $"Corso: {corso.Nome}";

        DatePickerInizio.Date = DateTime.Today;

        var opzioni = new List<string>();
        if (corso.CostoSingolo is > 0) opzioni.Add("Singolo");
        if (corso.CostoMensile is > 0) opzioni.Add("Mensile");
        if (corso.CostoAnnuale is > 0) opzioni.Add("Annuale");
        if (opzioni.Count == 0) opzioni.Add("Mensile");

        PickerTipo.ItemsSource = opzioni;
        PickerTipo.SelectedIndex = 0;

        AggiornaValoriStandard();
        DatePickerScadenza.Date = _scadenzaStandard;
        EntryPrezzo.Text = _importoStandard.ToString("0.##");
    }

    // La data di partenza scelta: quella personalizzata se attiva, altrimenti oggi
    private DateTime InizioCorrente => SwitchInizioPersonalizzato.IsToggled ? DatePickerInizio.Date : DateTime.Now;

    private void OnTipoChanged(object sender, EventArgs e)
    {
        _tipoAbbonamento = PickerTipo.SelectedItem?.ToString() ?? "Mensile";
        AggiornaValoriStandard();

        if (!SwitchScadenzaPersonalizzata.IsToggled)
            DatePickerScadenza.Date = _scadenzaStandard;

        if (!SwitchPrezzoPersonalizzato.IsToggled)
            EntryPrezzo.Text = _importoStandard.ToString("0.##");
    }

    private void AggiornaValoriStandard()
    {
        DateTime baseInizio = InizioCorrente;

        switch (_tipoAbbonamento)
        {
            case "Singolo":
                _importoStandard = _corso.CostoSingolo ?? 0;
                _scadenzaStandard = baseInizio.AddDays(1);
                break;
            case "Annuale":
                _importoStandard = _corso.CostoAnnuale ?? 0;
                _scadenzaStandard = baseInizio.AddYears(1);
                break;
            default:
                _tipoAbbonamento = "Mensile";
                _importoStandard = _corso.CostoMensile ?? 0;
                _scadenzaStandard = baseInizio.AddDays(28);
                break;
        }

        LblStandardInfo.Text = $"Standard: € {_importoStandard:N2} — dal {baseInizio:dd/MM/yyyy} al {_scadenzaStandard:dd/MM/yyyy}";
    }

    private void OnSwitchInizioToggled(object sender, ToggledEventArgs e)
    {
        DatePickerInizio.IsEnabled = e.Value;
        if (!e.Value) DatePickerInizio.Date = DateTime.Today;

        AggiornaValoriStandard();

        if (!SwitchScadenzaPersonalizzata.IsToggled)
            DatePickerScadenza.Date = _scadenzaStandard;
    }

    private void OnDataInizioChanged(object sender, DateChangedEventArgs e)
    {
        AggiornaValoriStandard();

        if (!SwitchScadenzaPersonalizzata.IsToggled)
            DatePickerScadenza.Date = _scadenzaStandard;
    }

    private void OnSwitchScadenzaToggled(object sender, ToggledEventArgs e)
    {
        DatePickerScadenza.IsEnabled = e.Value;
        if (!e.Value) DatePickerScadenza.Date = _scadenzaStandard;
    }

    private void OnSwitchPrezzoToggled(object sender, ToggledEventArgs e)
    {
        EntryPrezzo.IsEnabled = e.Value;
        if (!e.Value) EntryPrezzo.Text = _importoStandard.ToString("0.##");
    }

    private async void OnAnnullaClicked(object sender, EventArgs e)
        => await Navigation.PopModalAsync();

    private async void OnSalvaClicked(object sender, EventArgs e)
    {
        double importoFinale = _importoStandard;

        if (SwitchPrezzoPersonalizzato.IsToggled)
        {
            if (!double.TryParse(
                    EntryPrezzo.Text?.Replace(',', '.'),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out importoFinale))
            {
                await DisplayAlert("Attenzione", "Inserisci un prezzo valido.", "OK");
                return;
            }
        }

        DateTime inizioFinale = SwitchInizioPersonalizzato.IsToggled ? DatePickerInizio.Date : DateTime.Now;

        DateTime scadenzaFinale = SwitchScadenzaPersonalizzata.IsToggled
            ? DatePickerScadenza.Date
            : _scadenzaStandard;

        if (scadenzaFinale.Date < inizioFinale.Date)
        {
            await DisplayAlert("Attenzione", "La data di scadenza non può essere precedente alla data di inizio.", "OK");
            return;
        }

        if (scadenzaFinale.Date < DateTime.Today)
        {
            bool conferma = await DisplayAlert(
                "Attenzione",
                "La data di scadenza scelta è nel passato. Vuoi continuare comunque?",
                "Sì",
                "Annulla");
            if (!conferma) return;
        }

        var nuovo = new Abbonamenti
        {
            CorsoId = _corso.Id,
            Corso = _corso,
            TipoAbbonamento = _tipoAbbonamento,
            DataInizio = inizioFinale,
            DataScadenza = scadenzaFinale,
            ImportoTotale = importoFinale,
            ImportoPagato = importoFinale,
            IsPagato = 1,
            IsSospeso = 0,
            Attivo = 1
        };

        await Navigation.PopModalAsync();
        await _onSalva(nuovo);
    }
}