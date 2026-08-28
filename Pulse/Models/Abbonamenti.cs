using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pulse.Models;

public partial class Abbonamenti
{
    public int Id { get; set; }

    public int AllievoId { get; set; }

    public int CorsoId { get; set; }

    public string TipoAbbonamento { get; set; } = null!;

    public DateTime DataInizio { get; set; }

    public DateTime DataScadenza { get; set; }

    public double ImportoTotale { get; set; }

    public double ImportoPagato { get; set; }

    public int IsPagato { get; set; }

    public int IsSospeso { get; set; }

    public DateTime? DataSospensione { get; set; }

    public int GiorniRimanentiCongelati { get; set; }

    public int Attivo { get; set; }

    public virtual Allievi Allievo { get; set; } = null!;

    public virtual Corsi Corso { get; set; } = null!;
    

    // --- PROPRIETÀ CALCOLATE PER LA GRAFICA (NotMapped) ---
    [NotMapped]
    public double DaPagare => Math.Max(0, ImportoTotale - ImportoPagato);

    [NotMapped]
    public string StatoTesto
    {
        get
        {
            // 1. Sospeso / In Pausa
            if (IsSospeso == 1)
                return "Sospeso ⏸️";

            // 2. Scaduto
            if (DateTime.Now.Date > DataScadenza.Date)
                return "Scaduto ❌";

            // 3. In Scadenza (ultimi 5 giorni)
            if ((DataScadenza.Date - DateTime.Now.Date).TotalDays <= 5)
                return "In Scadenza ⏳";

            // 4. Attivo e regolare
            return "Attivo ✅";
        }
    }

    [NotMapped]
    public string ColoreStatoHex
    {
        get
        {
            // Giallo / Ambra per Sospeso
            if (IsSospeso == 1)
                return "#EAB308"; // Giallo acceso / Amber

            // Rosso per Scaduto
            if (DateTime.Now.Date > DataScadenza.Date)
                return "#EF4444";

            // Arancione per In Scadenza
            if ((DataScadenza.Date - DateTime.Now.Date).TotalDays <= 5)
                return "#F97316";

            // Verde per Attivo
            return "#10B981";
        }
    }

    [NotMapped]
    public string IconaPausaTesto => IsSospeso == 1 ? "▶️" : "⏸️";
}
