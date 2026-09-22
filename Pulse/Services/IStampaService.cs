namespace Pulse.Services;

/// <summary>
/// Stampa di documenti HTML senza passare dal browser.
/// Su Windows usa il motore WebView2 gia' presente nel sistema: la pagina viene
/// renderizzata fuori schermo e mandata direttamente alla stampante, oppure
/// salvata come PDF. Sulle altre piattaforme i metodi rispondono false e il
/// chiamante deve ricadere sull'apertura del file (Launcher).
/// </summary>
public interface IStampaService
{
    /// <summary>True solo dove la stampa diretta e' realmente disponibile (Windows).</summary>
    bool SupportaStampaSilenziosa { get; }

    /// <summary>
    /// Manda l'HTML alla stampante senza mostrare nessuna finestra di dialogo.
    /// </summary>
    /// <param name="html">Documento HTML completo.</param>
    /// <param name="nomeStampante">Nome della stampante; se vuoto si usa quella predefinita di Windows.</param>
    /// <returns>True se la stampa e' stata accettata dalla stampante.</returns>
    Task<bool> StampaHtmlAsync(string html, string? nomeStampante = null);

    /// <summary>
    /// Salva l'HTML come PDF nel percorso indicato, creando le cartelle mancanti.
    /// </summary>
    Task<bool> SalvaHtmlComePdfAsync(string html, string percorsoPdf);
}
