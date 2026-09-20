using System.ComponentModel.DataAnnotations.Schema;

namespace Pulse.Models;

public partial class Lezioni
{
    /// <summary>
    /// Durata della lezione in ore, calcolata dagli orari salvati come testo.
    /// Restituisce 0 se gli orari non sono leggibili o se la fine precede l'inizio.
    /// </summary>
    [NotMapped]
    public double DurataOre
    {
        get
        {
            if (TimeSpan.TryParse(OraInizio, out var inizio) && TimeSpan.TryParse(OraFine, out var fine))
            {
                var durata = fine - inizio;
                return durata.TotalHours > 0 ? durata.TotalHours : 0;
            }
            return 0;
        }
    }
}