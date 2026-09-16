using System.Net;
using System.Net.Mail;

namespace Pulse.Services;

public class EmailService : IEmailService
{
    private readonly IImpostazioniService _impostazioniService;

    public EmailService(IImpostazioniService impostazioniService)
    {
        _impostazioniService = impostazioniService;
    }

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
        System.Diagnostics.Debug.WriteLine("=================== EMAIL SIMULATA (DEBUG) ===================");
        System.Diagnostics.Debug.WriteLine($"Destinatari ({listaDestinatari.Count}): {string.Join(", ", listaDestinatari)}");
        System.Diagnostics.Debug.WriteLine($"Oggetto: {oggetto}");
        System.Diagnostics.Debug.WriteLine($"Corpo: {corpo}");
        System.Diagnostics.Debug.WriteLine("================================================================");
        return true;
#else
        try
        {
            var impostazioni = await _impostazioniService.GetImpostazioniAsync();

            var smtpHost = impostazioni.EmailSmtpHost ?? string.Empty;
            var smtpUser = impostazioni.EmailSmtpUser ?? string.Empty;
            var smtpPassword = impostazioni.EmailSmtpPassword ?? string.Empty;

            if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(smtpUser))
            {
                System.Diagnostics.Debug.WriteLine("⚠️ EmailService: email non configurata nelle Impostazioni.");
                return false;
            }

            var smtpPort = impostazioni.EmailSmtpPort ?? 587;
            var usaSsl = impostazioni.EmailUseSsl != 0;
            var mittenteNome = string.IsNullOrWhiteSpace(impostazioni.EmailMittenteNome) ? "Pulse" : impostazioni.EmailMittenteNome;

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

            foreach (var destinatario in listaDestinatari)
                messaggio.Bcc.Add(destinatario);

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

    public async Task<(bool Successo, string? Errore)> InviaEmailTestAsync(string indirizzoDestinatario)
    {
        try
        {
            var impostazioni = await _impostazioniService.GetImpostazioniAsync();

            var smtpHost = impostazioni.EmailSmtpHost ?? string.Empty;
            var smtpUser = impostazioni.EmailSmtpUser ?? string.Empty;
            var smtpPassword = impostazioni.EmailSmtpPassword ?? string.Empty;

            if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(smtpUser))
            {
                return (false, "Host SMTP o email non configurati.");
            }

            var smtpPort = impostazioni.EmailSmtpPort ?? 587;
            var usaSsl = impostazioni.EmailUseSsl != 0;
            var mittenteNome = string.IsNullOrWhiteSpace(impostazioni.EmailMittenteNome) ? "Pulse" : impostazioni.EmailMittenteNome;

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPassword),
                EnableSsl = usaSsl
            };

            using var messaggio = new MailMessage
            {
                From = new MailAddress(smtpUser, mittenteNome),
                Subject = "Test invio email - Pulse",
                Body = "Questa è una email di prova inviata dalle Impostazioni di Pulse. Se la ricevi, la configurazione è corretta.",
                IsBodyHtml = false
            };

            messaggio.To.Add(indirizzoDestinatario);

            await client.SendMailAsync(messaggio);
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}