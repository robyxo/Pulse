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
        return lezioni.Sum(CalcolaDurataOre);
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
                totaleOre += CalcolaDurataOre(lezione);
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

    private double CalcolaDurataOre(Lezioni lezione)
    {
        if (TimeSpan.TryParse(lezione.OraInizio, out var inizio) && TimeSpan.TryParse(lezione.OraFine, out var fine))
        {
            var durata = fine - inizio;
            return durata.TotalHours > 0 ? durata.TotalHours : 0;
        }
        return 0;
    }

    private DateTime? ParseData(string? data) =>
        DateTime.TryParse(data, out var d) ? d : null;
}