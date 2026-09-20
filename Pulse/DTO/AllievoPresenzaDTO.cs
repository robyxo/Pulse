using Pulse.Helpers;
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

            var stato = StatoAbbonamentoHelper.Calcola(Abbonamento);
            if (stato == StatoAbbonamento.InPausa)
                return StatoAbbonamentoHelper.GetEtichettaConIcona(stato);

            // Se ha pagato oggi è attivo, altrimenti è da saldare per la lezione
            if (Abbonamento.TipoAbbonamento == "Singolo")
                return Abbonamento.DataInizio.Date == DateTime.Today ? "Pagato Oggi ✅" : "Da Saldare 💶";

            return StatoAbbonamentoHelper.GetEtichettaConIcona(stato);
        }
    }

    public string ColoreStatoHex
    {
        get
        {
            if (Abbonamento == null) return "#EF4444";   // non iscritto: rosso

            var stato = StatoAbbonamentoHelper.Calcola(Abbonamento);

            if (stato != StatoAbbonamento.InPausa && Abbonamento.TipoAbbonamento == "Singolo")
            {
                return Abbonamento.DataInizio.Date == DateTime.Today
                    ? "#10B981"   // pagato oggi: verde
                    : "#3B82F6";  // da saldare: blu
            }

            return StatoAbbonamentoHelper.GetColore(stato);
        }
    }

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