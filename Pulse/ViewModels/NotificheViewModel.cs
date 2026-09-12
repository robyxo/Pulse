using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pulse.Services;
using Pulse.Utils;

namespace Pulse.ViewModels;

public partial class NotificheViewModel : BaseViewModel
{
    private readonly IDatabaseService _dbService;
    private readonly IEmailService _emailService;

    [ObservableProperty]
    private string _oggetto = "Comunicazione da Pulse";

    [ObservableProperty]
    private string _messaggio = string.Empty;

    public NotificheViewModel(INavigationService navigationService, IDatabaseService dbService, IEmailService emailService)
        : base(navigationService)
    {
        _dbService = dbService;
        _emailService = emailService;
        Title = "Notifiche";
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
}