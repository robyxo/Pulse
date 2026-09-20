using Pulse.Helpers;
using Pulse.Models;

namespace Pulse.DTO;

public class LezioneMaestroDTO
{

    public Lezioni Lezione { get; }

    public LezioneMaestroDTO(Lezioni lezione)
    {
        Lezione = lezione;
    }

    public string NomeCorso => Lezione.Corso?.Nome ?? "-";

    public string GiornoTesto => DateHelper.GetNomeGiornoDaDb(Lezione.GiornoSettimana, "-");

    public string OrarioTesto => $"{Lezione.OraInizio} - {Lezione.OraFine}";

    public double DurataOre => Lezione.DurataOre;

    public string DurataTesto => $"{DurataOre:0.##} h";
}