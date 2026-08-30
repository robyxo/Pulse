using Pulse.Models;

namespace Pulse.DTO;

public class AllievoTabellaDTO
{
    public Allievi Allievo { get; set; } = null!;
    public Abbonamenti? UltimoAbbonamento { get; set; }

    public string NomeCompleto => $"{Allievo.Nome} {Allievo.Cognome}".Trim();
    public string Telefono => !string.IsNullOrWhiteSpace(Allievo.Telefono) ? Allievo.Telefono : "-";
    public string CorsoNome => UltimoAbbonamento?.Corso?.Nome ?? "Nessun Corso";
    public string TipoAbbonamento => UltimoAbbonamento?.TipoAbbonamento ?? "-";
    public string ScadenzaTesto => UltimoAbbonamento != null ? UltimoAbbonamento.DataScadenza.ToString("dd/MM/yyyy") : "-";

    public string StatoChiave
    {
        get
        {
            if (UltimoAbbonamento == null) return "Nessuno";
            if (UltimoAbbonamento.IsSospeso == 1) return "In Pausa";
            if (DateTime.Now.Date > UltimoAbbonamento.DataScadenza.Date) return "Scaduto";
            if ((UltimoAbbonamento.DataScadenza.Date - DateTime.Now.Date).TotalDays <= 5) return "In Scadenza";
            return "Attivo";
        }
    }

    // Colori dei pallini di stato
    public string ColorePallinoHex => StatoChiave switch
    {
        "Attivo" => "#10B981",      // 🟢 Verde
        "In Scadenza" => "#F59E0B", // 🟡 Giallo
        "In Pausa" => "#06B6D4",    // 🔵 Azzurro
        "Scaduto" => "#EF4444",     // 🔴 Rosso
        _ => "#94A3B8"              // ⚪ Grigio
    };
}