using Pulse.Models;
using Pulse.Services;

namespace Pulse.Views;

public partial class GestioneLezionePage : ContentPage
{
    private readonly Lezioni _lezione;
    private readonly IDatabaseService _db;

    public GestioneLezionePage(Lezioni lezione, IDatabaseService db)
    {
        InitializeComponent();
        _lezione = lezione;
        _db = db;
        CaricaDati();
    }

    private async void CaricaDati()
    {
        var corsi = await _db.GetCorsiAttivi();
        var maestri = await _db.GetInsegnantiAttivi();

        PickerCorsi.ItemsSource = corsi;
        PickerMaestri.ItemsSource = maestri;

        // Seleziona Corso e Maestro esistenti
        if (_lezione.Corso != null)
        {
            PickerCorsi.SelectedItem = corsi.FirstOrDefault(c => c.Id == _lezione.CorsoId);
        }

        if (_lezione.Insegnante != null)
        {
            PickerMaestri.SelectedItem = maestri.FirstOrDefault(m => m.Id == _lezione.InsegnanteId);
        }

        // Imposta orari
        if (TimeSpan.TryParse(_lezione.OraInizio, out var tInizio))
            TimeInizio.Time = tInizio;

        if (TimeSpan.TryParse(_lezione.OraFine, out var tFine))
            TimeFine.Time = tFine;

        // Carica subito gli allievi del corso corrente
        await CaricaAllieviCorsoSelezionato();
    }

    /// <summary>
    /// Scatta quando l'utente cambia il corso nel Picker
    /// </summary>
    private async void OnCorsoChanged(object sender, EventArgs e)
    {
        await CaricaAllieviCorsoSelezionato();
    }

    private async Task CaricaAllieviCorsoSelezionato()
    {
        if (PickerCorsi.SelectedItem is Corsi corsoSelezionato)
        {
            // Recupera la lista degli allievi dal DB per il corso selezionato
            var allievi = await _db.GetAllieviPerCorso(corsoSelezionato.Id);

            if (allievi != null && allievi.Any())
            {
                ListaAllievi.ItemsSource = allievi;
                ListaAllievi.IsVisible = true;
                LblNessunAllievo.IsVisible = false;
                LblTitoloAllievi.Text = $"👥 Allievi Iscritti ({allievi.Count})";
            }
            else
            {
                ListaAllievi.ItemsSource = null;
                ListaAllievi.IsVisible = false;
                LblNessunAllievo.IsVisible = true;
                LblTitoloAllievi.Text = "👥 Allievi Iscritti (0)";
            }
        }
    }

    private async void OnSalvaClicked(object sender, EventArgs e)
    {
        if (PickerCorsi.SelectedItem == null || PickerMaestri.SelectedItem == null)
        {
            await DisplayAlert("Attenzione", "Seleziona sia un corso che un maestro prima di salvare.", "OK");
            return;
        }

        _lezione.CorsoId = ((Corsi)PickerCorsi.SelectedItem).Id;
        _lezione.InsegnanteId = ((Insegnanti)PickerMaestri.SelectedItem).Id;
        _lezione.OraInizio = TimeInizio.Time.ToString(@"hh\:mm");
        _lezione.OraFine = TimeFine.Time.ToString(@"hh\:mm");

        await _db.SalvaLezione(_lezione);
        await Navigation.PopAsync();
    }
}