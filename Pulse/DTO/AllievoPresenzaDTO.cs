using Pulse.Models;

namespace Pulse.DTO;

public class AllievoPresenzaDTO
{
    public Allievi Allievo { get; set; } = null!;
    public Abbonamenti? Abbonamento { get; set; }

    public string NomeCompleto => $"{Allievo.Nome} {Allievo.Cognome}".Trim();
    public string TipoAbbonamento => Abbonamento?.TipoAbbonamento ?? "Nessun Abbonamento";

    // Stato specifico per la lezione
    public string StatoTesto
    {
        get
        {
            if (Abbonamento == null) return "Non Iscritto ❌";
            if (Abbonamento.IsSospeso == 1) return "In Pausa ⏸️";

            if (Abbonamento.TipoAbbonamento == "Singolo")
            {
                // Se ha pagato oggi è attivo, altrimenti è da saldare per la lezione
                return Abbonamento.DataInizio.Date == DateTime.Today ? "Pagato Oggi ✅" : "Da Saldare 💶";
            }

            if (DateTime.Today > Abbonamento.DataScadenza.Date) return "Scaduto ❌";
            if ((Abbonamento.DataScadenza.Date - DateTime.Today).TotalDays <= 5) return "In Scadenza ⏳";

            return "Attivo ✅";
        }
    }

    public string ColoreStatoHex => StatoTesto switch
    {
        "Pagato Oggi ✅" or "Attivo ✅" => "#10B981", // Verde
        "In Scadenza ⏳" => "#F59E0B",                 // Giallo/Arancio
        "Da Saldare 💶" => "#3B82F6",                  // Blu
        "In Pausa ⏸️" => "#06B6D4",                   // Cyan
        _ => "#EF4444"                                // Rosso
    };

    // Visibilità Tasto Incasso
    public bool MostraPulsanteIncasso
    {
        get
        {
            if (Abbonamento == null) return true;
            if (Abbonamento.IsSospeso == 1 || Abbonamento.Attivo == 0) return false;

            // Singolo: compare se non ha saldato nella giornata odierna
            if (Abbonamento.TipoAbbonamento == "Singolo")
            {
                return Abbonamento.DataInizio.Date < DateTime.Today;
            }

            // Mensile/Annuale: compare se scaduto o negli ultimi 5 giorni
            return (Abbonamento.DataScadenza.Date - DateTime.Today).TotalDays <= 5;
        }
    }

    public string TestoPulsanteIncasso =>
        Abbonamento?.TipoAbbonamento == "Singolo" ? "💶 Paga Oggi" : "🔄 Rinnova";
}