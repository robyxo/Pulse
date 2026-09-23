namespace Pulse.Services;

/// <summary>
/// Avvisi in tempo reale per la segreteria quando qualcuno si registra da un
/// dispositivo (tablet in sala).
/// </summary>
public interface IAvvisiDispositivoService
{
    /// <summary>
    /// Da chiamare appena il dispositivo ha salvato un nuovo allievo.
    /// Mostra un popup sul PC ("Si è appena registrato Mario Rossi") e, se la
    /// segreteria conferma, apre subito la sua scheda per fare l'abbonamento.
    ///
    /// Si può chiamare da qualunque thread: il popup viene sempre mostrato su
    /// quello dell'interfaccia, e più registrazioni ravvicinate vengono mostrate
    /// una alla volta invece di sovrapporsi.
    /// </summary>
    Task SegnalaNuovaRegistrazioneAsync(int allievoId);
}
