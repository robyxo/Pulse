using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pulse.DTO;
using Pulse.Models;
using Pulse.Services;
using Pulse.Utils;
using System.Collections.ObjectModel;

namespace Pulse.ViewModels;

public partial class CalendarioEventiViewModel : BaseViewModel
{
    private readonly IDatabaseService _dbService;
    private readonly IEmailService _emailService;

    private CalendarioChiusure? _chiusuraInModifica;

    [ObservableProperty]
    private ObservableCollection<ChiusuraEventoDTO> _listaChiusureEventi = new();

    [ObservableProperty]
    private bool _isModifica;

    public List<string> OpzioniTipo { get; } = new() { "Chiusura", "Evento" };

    [ObservableProperty]
    private string _tipoSelezionato = "Chiusura";

    [ObservableProperty]
    private DateTime _dataInizioForm = DateTime.Today;

    [ObservableProperty]
    private DateTime _dataFineForm = DateTime.Today;

    [ObservableProperty]
    private string _motivoForm = string.Empty;

    public CalendarioEventiViewModel(IDatabaseService dbService, IEmailService emailService)
    {
        _dbService = dbService;
        _emailService = emailService;
        Title = "Calendario Eventi";
    }

    public async Task CaricaDatiAsync()
    {
        await EseguiConCaricamento(CaricaDatiInternoAsync);
    }

    private async Task CaricaDatiInternoAsync()
    {
        var chiusure = await _dbService.GetChiusureAsync();

        var lista = chiusure
            .OrderByDescending(c => c.DataInizio)
            .Select(c => new ChiusuraEventoDTO(c))
            .ToList();

        ListaChiusureEventi = new ObservableCollection<ChiusuraEventoDTO>(lista);
    }

    [RelayCommand]
    public void NuovoElemento()
    {
        ResetForm();
    }

    private void ResetForm()
    {
        _chiusuraInModifica = null;
        IsModifica = false;
        TipoSelezionato = "Chiusura";
        DataInizioForm = DateTime.Today;
        DataFineForm = DateTime.Today;
        MotivoForm = string.Empty;
    }

    [RelayCommand]
    public void ModificaElemento(ChiusuraEventoDTO item)
    {
        if (item == null) return;

        _chiusuraInModifica = item.Chiusura;
        IsModifica = true;
        TipoSelezionato = item.Tipo;
        DataInizioForm = item.DataInizioDate ?? DateTime.Today;
        DataFineForm = item.DataFineDate ?? DateTime.Today;
        MotivoForm = item.Motivo;
    }

    [RelayCommand]
    public async Task EliminaElementoAsync(ChiusuraEventoDTO item)
    {
        if (item == null) return;

        string avviso = item.IsEvento
            ? $"Vuoi eliminare l'evento \"{item.Motivo}\"?"
            : $"Vuoi eliminare la chiusura \"{item.Motivo}\"?\n\nAttenzione: gli abbonamenti eventualmente già estesi per questa chiusura NON verranno riportati automaticamente alla scadenza originale.";

        bool conferma = await AlertPopup.ShowConfirmation("Conferma Eliminazione", avviso, "Sì, Elimina", "Annulla");
        if (!conferma) return;

        await EseguiConCaricamento(async () =>
        {
            await _dbService.EliminaChiusuraAsync(item.Id);

            if (_chiusuraInModifica?.Id == item.Id)
                ResetForm();

            await CaricaDatiInternoAsync();
        });
    }

    [RelayCommand]
    public async Task SalvaAsync()
    {
        if (string.IsNullOrWhiteSpace(MotivoForm))
        {
            await AlertPopup.ShowWarning("Inserisci una descrizione/motivo.");
            return;
        }

        if (DataFineForm.Date < DataInizioForm.Date)
        {
            await AlertPopup.ShowWarning("La data 'Al' non può essere precedente alla data 'Dal'.");
            return;
        }

        var chiusura = _chiusuraInModifica ?? new CalendarioChiusure();
        bool eraNuova = chiusura.Id == 0;
        bool eraEvento = TipoSelezionato == "Evento";

        chiusura.Tipo = TipoSelezionato;
        chiusura.DataInizio = DataInizioForm.Date.ToString("yyyy-MM-dd");
        chiusura.DataFine = DataFineForm.Date.ToString("yyyy-MM-dd");
        chiusura.Motivo = MotivoForm.Trim();
        chiusura.Stato = 1;

        bool salvataggioRiuscito = false;

        await EseguiConCaricamento(async () =>
        {
            var (successo, abbonamentiEstesi) = await _dbService.SalvaChiusuraAsync(chiusura);

            if (!successo)
            {
                await AlertPopup.ShowError("Non è stato possibile salvare.");
                return;
            }

            salvataggioRiuscito = true;

            if (!eraEvento && eraNuova)
            {
                string esito = abbonamentiEstesi > 0
                    ? $"Chiusura salvata.\n{abbonamentiEstesi} abbonamenti attivi sono stati automaticamente prolungati."
                    : "Chiusura salvata. Nessun abbonamento attivo risultava sovrapposto al periodo.";

                await AlertPopup.Show("Fatto", esito);
            }
            else
            {
                await AlertPopup.Show("Fatto", "Salvato con successo.");
            }

            ResetForm();
            await CaricaDatiInternoAsync();
        });

        // Fuori dal blocco di caricamento: lo spinner è già spento
        // mentre l'utente decide se inviare l'email agli allievi.
        if (salvataggioRiuscito && eraNuova && eraEvento)
        {
            await ProponiInvioEmailAsync(chiusura);
        }
    }

    private async Task ProponiInvioEmailAsync(CalendarioChiusure chiusura)
    {
        bool vuoleInviare = await AlertPopup.ShowConfirmation(
            "Invia Email",
            $"Vuoi inviare una email a tutti gli allievi per segnalare l'evento \"{chiusura.Motivo}\"?",
            "Sì, Invia",
            "No");

        if (!vuoleInviare) return;

        await EseguiConCaricamento(async () =>
        {
            var allievi = await _dbService.GetAllieviAttiviAsync();

            var destinatari = allievi
                .Where(a => !string.IsNullOrWhiteSpace(a.Email))
                .Select(a => a.Email!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (destinatari.Count == 0)
            {
                await AlertPopup.ShowWarning("Nessun allievo con un indirizzo email salvato.");
                return;
            }

            string dataTesto = DateTime.TryParse(chiusura.DataInizio, out var d)
                ? d.ToString("dd/MM/yyyy")
                : chiusura.DataInizio ?? "";

            string oggetto = $"Evento: {chiusura.Motivo}";
            string messaggio = $"Ciao!\n\nTi segnaliamo un evento in programma il {dataTesto}: {chiusura.Motivo}.\n\nA presto!";

            bool esito = await _emailService.InviaEmailAsync(destinatari, oggetto, messaggio);

            if (esito)
            {
                chiusura.EmailInviata = 1;
                await _dbService.SalvaChiusuraAsync(chiusura);
                await AlertPopup.Show("Fatto", $"Email inviata a {destinatari.Count} allievi.");
                await CaricaDatiInternoAsync();
            }
            else
            {
                await AlertPopup.ShowError("Non è stato possibile inviare l'email. Controlla la configurazione email nelle Impostazioni.");
            }
        });
    }
}