namespace Pulse.Helpers;

/// <summary>
/// Valori ammessi per il campo Tipo di CalendarioChiusure.
/// Sono costanti condivise fra servizio e ViewModel: il confronto avviene
/// su queste stringhe, quindi non vanno cambiate senza aggiornare i dati.
/// </summary>
public static class TipiEvento
{
    /// <summary>Chiusura normale: gli abbonamenti si prolungano, se il recupero è attivo.</summary>
    public const string Chiusura = "Chiusura";

    /// <summary>Fine stagione: gli abbonamenti scadono il giorno di chiusura, non si prolungano.</summary>
    public const string ChiusuraStagionale = "Chiusura stagionale";

    /// <summary>Evento informativo: non tocca gli abbonamenti.</summary>
    public const string Evento = "Evento";
}
