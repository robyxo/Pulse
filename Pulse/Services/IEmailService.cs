namespace Pulse.Services;

public interface IEmailService
{
    /// <summary>
    /// Invia lo stesso messaggio a una lista di destinatari (in BCC).
    /// In DEBUG l'invio è simulato (solo log in output), in RELEASE usa il provider SMTP
    /// configurato in appsettings.json (sezione "EmailSettings").
    /// </summary>
    Task<bool> InviaEmailAsync(IEnumerable<string> destinatari, string oggetto, string corpo);
}