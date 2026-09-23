namespace Pulse.DTO;

/// <summary>
/// Definizione di un campo extra, letta dal modello HTML della scuola.
///
/// Sintassi nel modello:
///   {{EXTRA:PROFESSIONE|Professione|testo}}
///   {{EXTRA:CONOSCENZA|Come ci hai conosciuto|scelta:RADIO,FACEBOOK,AMICO}}
///   {{EXTRA:PROFESSIONE}}                     (etichetta = chiave, tipo = testo)
///
/// Un file solo definisce come si stampa il modulo, cosa chiede il tablet e con
/// quale chiave la risposta finisce nelle statistiche.
/// </summary>
public class CampoExtraDTO
{
    /// <summary>Codice del campo, usato come chiave in archivio. Es. CONOSCENZA.</summary>
    public string Chiave { get; set; } = string.Empty;

    /// <summary>Testo mostrato all'allievo sul tablet e nelle statistiche.</summary>
    public string Etichetta { get; set; } = string.Empty;

    /// <summary>Risposte ammesse. Vuoto = testo libero.</summary>
    public List<string> Opzioni { get; set; } = new();

    public bool IsScelta => Opzioni.Count > 0;

    /// <summary>Segnaposto completo così com'è scritto nel modello, per sostituirlo.</summary>
    public string Segnaposto { get; set; } = string.Empty;
}
