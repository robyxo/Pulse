using Pulse.DTO;
using Pulse.Models;

namespace Pulse.Services;

public interface IStatisticheService
{
    /// <summary>
    /// Anno scolastico che contiene la data indicata: dal 1 settembre al 31 agosto.
    /// </summary>
    (DateTime Dal, DateTime Al) AnnoScolastico(DateTime data);

    /// <summary>Tutti i numeri della pagina Statistiche per il periodo indicato.</summary>
    Task<RiepilogoStatisticheDTO> GetRiepilogoAsync(DateTime dal, DateTime al);

    // ================================================
    // QUERY SALVATE
    // ================================================

    /// <summary>
    /// Query salvate attive, in ordine. Quelle "solo amministratore" si vedono
    /// solo se richiesto (in Pulse: solo quando l'app gira da Visual Studio).
    /// </summary>
    Task<List<QuerySalvate>> GetQuerySalvateAsync(bool includiSoloAmministratore);

    Task<bool> SalvaQueryAsync(QuerySalvate query);

    Task<bool> EliminaQueryAsync(int id);

    /// <summary>
    /// Esegue una query di sola lettura. Il database viene aperto in modalita'
    /// "solo lettura": anche una query scritta male non puo' modificare i dati.
    /// </summary>
    Task<RisultatoQueryDTO> EseguiQueryAsync(string testoQuery, int queryId = 0, int maxRighe = 1000);
}
