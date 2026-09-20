namespace Pulse.DTO;

// Banda oraria della vista calendario "a bande" (Mattina / Pomeriggio / Sera).
public class FasciaOraria
{
    public string Nome { get; set; } = string.Empty;
    public TimeSpan OraInizio { get; set; }
    public TimeSpan OraFine { get; set; }

    public string TitoloFormattato => $"{Nome}\n({OraInizio:hh\\:mm} - {OraFine:hh\\:mm})";
}