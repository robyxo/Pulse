namespace Pulse.DTO;

public class FasciaOraria
{
    public string Nome { get; set; } = string.Empty;
    public TimeSpan OraInizio { get; set; }
    public TimeSpan OraFine { get; set; }
    public TimeSpan Orario { get; set; }
    public bool IsAttiva { get; set; } = true;

    public string TitoloFormattato => $"{Nome}\n({OraInizio:hh\\:mm} - {OraFine:hh\\:mm})";
}
