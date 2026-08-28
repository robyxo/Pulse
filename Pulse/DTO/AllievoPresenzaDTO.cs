using Pulse.Models;

namespace Pulse.DTO;

public class AllievoPresenzaDTO
{
    public Allievi Allievo { get; set; } = null!;
    public Abbonamenti? Abbonamento { get; set; }

    public string NomeCompleto => $"{Allievo.Nome} {Allievo.Cognome}";
    public string TipoAbbonamento => Abbonamento?.TipoAbbonamento ?? "Nessun Abbonamento";
    public string StatoTesto => Abbonamento?.StatoTesto ?? "Non Iscritto ❌";
    public string ColoreStatoHex => Abbonamento?.ColoreStatoHex ?? "#EF4444";

    // Utile per segnare la presenza o il pagamento al volo
    public bool Presente { get; set; }
    public bool RichiedePagamentoSingolo => Abbonamento?.TipoAbbonamento == "Singolo" && (Abbonamento?.DaPagare > 0);
}