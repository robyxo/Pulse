using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pulse.DTO;
using Pulse.Services;
using Pulse.Utils;
using System.Collections.ObjectModel;

namespace Pulse.ViewModels;

public partial class NotificheViewModel : BaseViewModel
{
    private readonly IDatabaseService _dbService;
    private readonly IEmailService _emailService;

    [ObservableProperty]
    private string _oggetto = "Comunicazione da Pulse";

    [ObservableProperty]
    private string _messaggio = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ComunicazioneRigaDTO> _listaComunicazioni = new();

    [ObservableProperty]
    private bool _nessunaComunicazione = true;

    public NotificheViewModel(IDatabaseService dbService, IEmailService emailService)
    {
        _dbService = dbService;
        _emailService = emailService;
        Title = "Notifiche";
    }

    public async Task CaricaComunicazioniAsync()
    {
        var comunicazioni = await _dbService.GetComunicazioniAsync();

        var righe = comunicazioni
            .Select(c => new ComunicazioneRigaDTO { Comunicazione = c })
            .ToList();

        ListaComunicazioni = new ObservableCollection<ComunicazioneRigaDTO>(righe);
        NessunaComunicazione = righe.Count == 0;
    }

    [RelayCommand]
    public async Task InviaNotificaAsync()
    {
        if (string.IsNullOrWhiteSpace(Messaggio))
        {
            await AlertPopup.ShowWarning("Scrivi un messaggio prima di inviare la notifica.");
            return;
        }

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

            bool conferma = await AlertPopup.ShowConfirmation(
                "Conferma Invio",
                $"Vuoi inviare il messaggio a {destinatari.Count} allievi?",
                "Sì, Invia",
                "Annulla");

            if (!conferma) return;

            bool esito = await _emailService.InviaEmailAsync(destinatari, Oggetto, Messaggio);

            // Si registra anche il tentativo fallito: sapere che una comunicazione
            // NON è partita è importante quanto sapere che è partita.
            await _dbService.RegistraComunicazioneAsync(
                "Email",
                Oggetto,
                Messaggio,
                destinatari,
                esito,
                esito ? null : "Invio non riuscito: controllare la configurazione email.");

            await CaricaComunicazioniAsync();

            if (esito)
            {
                await AlertPopup.Show("Fatto", $"Notifica inviata a {destinatari.Count} allievi.");
                Messaggio = string.Empty;
            }
            else
            {
                await AlertPopup.ShowError("Non è stato possibile inviare la notifica. Controlla la configurazione email.");
            }
        });
    }

    [RelayCommand]
    public async Task MostraDettaglioAsync(ComunicazioneRigaDTO riga)
    {
        if (riga == null) return;

        string destinatari = riga.NumeroDestinatari == 0
            ? "Nessun destinatario registrato."
            : string.Join("\n", riga.Destinatari);

        string esito = riga.Riuscita
            ? "Inviata"
            : $"NON inviata — {riga.Comunicazione.MessaggioErrore ?? "motivo non registrato"}";

        await AlertPopup.Show(
            riga.Oggetto,
            $"{riga.DataTesto} · {esito}\n\n{riga.Comunicazione.Corpo}\n\n— Destinatari ({riga.NumeroDestinatari}) —\n{destinatari}");
    }
}
