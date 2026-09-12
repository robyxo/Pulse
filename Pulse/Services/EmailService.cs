using System.Net;
using System.Net.Mail;

namespace Pulse.Services;

public class EmailService : IEmailService
{
    // Chiavi Preferences che verranno popolate dalla futura pagina Impostazioni
    private const string ChiaveSmtpHost = "Email_SmtpHost";
    private const string ChiaveSmtpPort = "Email_SmtpPort";
    private const string ChiaveSmtpUser = "Email_SmtpUser";
    private const string ChiaveSmtpPassword = "Email_SmtpPassword";
    private const string ChiaveMittenteNome = "Email_MittenteNome";
    private const string ChiaveUseSsl = "Email_UseSsl";

    public async Task<bool> InviaEmailAsync(IEnumerable<string> destinatari, string oggetto, string corpo)
    {
        var listaDestinatari = destinatari?
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? new List<string>();

        if (listaDestinatari.Count == 0)
        {
            System.Diagnostics.Debug.WriteLine("⚠️ EmailService: nessun destinatario valido, invio annullato.");
            return false;
        }

#if DEBUG
        // 🧪 MODALITÀ DEBUG/LOCALE: nessun invio reale, solo simulazione via log.
        System.Diagnostics.Debug.WriteLine("=================== EMAIL SIMULATA (DEBUG) ===================");
        System.Diagnostics.Debug.WriteLine($"Destinatari ({listaDestinatari.Count}): {string.Join(", ", listaDestinatari)}");
        System.Diagnostics.Debug.WriteLine($"Oggetto: {oggetto}");
        System.Diagnostics.Debug.WriteLine($"Corpo: {corpo}");
        System.Diagnostics.Debug.WriteLine("================================================================");
        return true;
#else
        try
        {
            var smtpHost = Preferences.Get(ChiaveSmtpHost, string.Empty);
            var smtpUser = Preferences.Get(ChiaveSmtpUser, string.Empty);
            var smtpPassword = Preferences.Get(ChiaveSmtpPassword, string.Empty);

            if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(smtpUser))
            {
                System.Diagnostics.Debug.WriteLine("⚠️ EmailService: email non configurata nelle Impostazioni.");
                return false;
            }

            var smtpPort = Preferences.Get(ChiaveSmtpPort, 587);
            var usaSsl = Preferences.Get(ChiaveUseSsl, true);
            var mittenteNome = Preferences.Get(ChiaveMittenteNome, "Pulse");

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPassword),
                EnableSsl = usaSsl
            };

            using var messaggio = new MailMessage
            {
                From = new MailAddress(smtpUser, mittenteNome),
                Subject = oggetto,
                Body = corpo,
                IsBodyHtml = false
            };

            // BCC così gli allievi non vedono gli indirizzi degli altri
            foreach (var destinatario in listaDestinatari)
                messaggio.Bcc.Add(destinatario);

            // Molti server SMTP richiedono almeno un "To" valido
            messaggio.To.Add(messaggio.From);

            await client.SendMailAsync(messaggio);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ EmailService: errore durante l'invio - {ex.Message}");
            return false;
        }
#endif
    }
}