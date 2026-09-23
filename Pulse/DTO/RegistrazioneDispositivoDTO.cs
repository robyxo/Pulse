namespace Pulse.DTO;

/// <summary>
/// Dati inviati dalla pagina di registrazione del tablet (JSON).
/// I nomi corrispondono ai "name" dei campi del modulo HTML.
/// </summary>
public class RegistrazioneDispositivoDTO
{
    /// <summary>Chiave del QR code: senza quella giusta la registrazione viene rifiutata.</summary>
    public string? Chiave { get; set; }

    public string? Nome { get; set; }
    public string? Cognome { get; set; }
    public string? CodiceFiscale { get; set; }

    /// <summary>Formato yyyy-MM-dd, come lo manda il campo data del browser.</summary>
    public string? DataNascita { get; set; }

    public string? Sesso { get; set; }
    public string? Indirizzo { get; set; }
    public string? Civico { get; set; }
    public string? Cap { get; set; }
    public string? Citta { get; set; }
    public string? Provincia { get; set; }
    public string? Telefono { get; set; }
    public string? Cellulare { get; set; }
    public string? Email { get; set; }

    /// <summary>
    /// Risposte alle domande extra del modello dispositivo, per chiave
    /// (es. PROFESSIONE, CONOSCENZA). Vuoto se la pagina non le chiedeva.
    /// </summary>
    public Dictionary<string, string>? Extra { get; set; }
}
