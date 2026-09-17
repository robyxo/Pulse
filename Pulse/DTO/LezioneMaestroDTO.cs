using Pulse.Models;

namespace Pulse.DTO;

public class LezioneMaestroDTO
{
    private static readonly string[] GiorniSettimana =
    {
        "Lunedì", "Martedì", "Mercoledì", "Giovedì", "Venerdì", "Sabato", "Domenica"
    };

    public Lezioni Lezione { get; }

    public LezioneMaestroDTO(Lezioni lezione)
    {
        Lezione = lezione;
    }

    public string NomeCorso => Lezione.Corso?.Nome ?? "-";

    public string GiornoTesto => (Lezione.GiornoSettimana >= 1 && Lezione.GiornoSettimana <= 7)
        ? GiorniSettimana[Lezione.GiornoSettimana - 1]
        : "-";

    public string OrarioTesto => $"{Lezione.OraInizio} - {Lezione.OraFine}";

    public double DurataOre
    {
        get
        {
            if (TimeSpan.TryParse(Lezione.OraInizio, out var inizio) && TimeSpan.TryParse(Lezione.OraFine, out var fine))
            {
                var durata = fine - inizio;
                return durata.TotalHours > 0 ? durata.TotalHours : 0;
            }
            return 0;
        }
    }

    public string DurataTesto => $"{DurataOre:0.##} h";
}