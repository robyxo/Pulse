using Pulse.Models;

namespace Pulse.Services;

public class CompensiMaestriService
{
    private readonly IDatabaseService _dbService;

    public CompensiMaestriService(IDatabaseService dbService)
    {
        _dbService = dbService;
    }

    public double CalcolaOreSettimanali(List<Lezioni> lezioni)
    {
        if (lezioni == null) return 0;
        return lezioni.Sum(l => l.DurataOre);
    }

    public async Task<double> CalcolaOrePeriodoAsync(List<Lezioni> lezioni, DateTime dataInizio, DateTime dataFine)
    {
        if (lezioni == null || lezioni.Count == 0 || dataFine < dataInizio) return 0;

        var chiusure = await _dbService.GetChiusureAsync();

        var periodiChiusura = chiusure
            .Where(c => string.Equals(c.Tipo, "Chiusura", StringComparison.OrdinalIgnoreCase))
            .Select(c => (Inizio: ParseData(c.DataInizio), Fine: ParseData(c.DataFine)))
            .Where(p => p.Inizio.HasValue && p.Fine.HasValue)
            .Select(p => (Inizio: p.Inizio!.Value.Date, Fine: p.Fine!.Value.Date))
            .ToList();

        double totaleOre = 0;

        for (DateTime giorno = dataInizio.Date; giorno <= dataFine.Date; giorno = giorno.AddDays(1))
        {
            bool giornoChiuso = periodiChiusura.Any(p => giorno >= p.Inizio && giorno <= p.Fine);
            if (giornoChiuso) continue;

            int giornoDb = (int)giorno.DayOfWeek == 0 ? 7 : (int)giorno.DayOfWeek;

            foreach (var lezione in lezioni.Where(l => l.GiornoSettimana == giornoDb))
            {
                totaleOre += lezione.DurataOre;
            }
        }

        return totaleOre;
    }

    public Task<double> CalcolaOreMeseAsync(List<Lezioni> lezioni, int anno, int mese)
    {
        var dataInizio = new DateTime(anno, mese, 1);
        var dataFine = dataInizio.AddMonths(1).AddDays(-1);
        return CalcolaOrePeriodoAsync(lezioni, dataInizio, dataFine);
    }

    public Task<double> CalcolaOreAnnoAsync(List<Lezioni> lezioni, int anno)
    {
        var dataInizio = new DateTime(anno, 1, 1);
        var dataFine = new DateTime(anno, 12, 31);
        return CalcolaOrePeriodoAsync(lezioni, dataInizio, dataFine);
    }

    /// <summary>
    /// Ore dell'anno scolastico (1 settembre - 31 agosto) che contiene il mese indicato.
    /// </summary>
    public Task<double> CalcolaOreAnnoScolasticoAsync(List<Lezioni> lezioni, int anno, int mese)
    {
        var (dataInizio, dataFine) = AnnoScolastico(anno, mese);
        return CalcolaOrePeriodoAsync(lezioni, dataInizio, dataFine);
    }

    public static (DateTime Dal, DateTime Al) AnnoScolastico(int anno, int mese)
    {
        int annoInizio = mese >= 9 ? anno : anno - 1;
        return (new DateTime(annoInizio, 9, 1), new DateTime(annoInizio + 1, 8, 31));
    }

    private DateTime? ParseData(string? data) =>
        DateTime.TryParse(data, out var d) ? d : null;
}