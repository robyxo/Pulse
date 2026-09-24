using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Pulse.DTO;
using Pulse.Helpers;
using Pulse.Models;

namespace Pulse.Services;

public class StatisticheService : IStatisticheService
{
    private static readonly CultureInfo Italiano = CultureInfo.GetCultureInfo("it-IT");

    private readonly IDbContextFactory<PulseContext> _contextFactory;
    private readonly IDatabaseService _dbService;
    private readonly CompensiMaestriService _compensiService;

    public StatisticheService(
        IDbContextFactory<PulseContext> contextFactory,
        IDatabaseService dbService,
        CompensiMaestriService compensiService)
    {
        _contextFactory = contextFactory;
        _dbService = dbService;
        _compensiService = compensiService;
    }

    public (DateTime Dal, DateTime Al) AnnoScolastico(DateTime data) =>
        CompensiMaestriService.AnnoScolastico(data.Year, data.Month);

    // Data in cui l'abbonamento e' stato incassato. Per quelli vecchi, registrati
    // prima che esistesse il campo, vale la data di inizio.
    private static DateTime DataIncasso(Abbonamenti a) => (a.DataPagamento ?? a.DataInizio).Date;

    private static bool Stornato(Abbonamenti a) => a.Stornato == 1;

    // ================================================
    // RIEPILOGO
    // ================================================

    public async Task<RiepilogoStatisticheDTO> GetRiepilogoAsync(DateTime dal, DateTime al)
    {
        dal = dal.Date;
        al = al.Date;
        if (al < dal) (dal, al) = (al, dal);

        await using var context = await _contextFactory.CreateDbContextAsync();

        // Una scuola di danza ha qualche migliaio di abbonamenti al massimo:
        // si caricano tutti e si filtra in memoria, cosi' le date (salvate come
        // testo da SQLite) e i valori nulli dei record vecchi non danno sorprese.
        var abbonamenti = await context.Abbonamentis
            .AsNoTracking()
            .Include(a => a.Corso)
            .Where(a => a.Attivo == 1)
            .ToListAsync();

        var validi = abbonamenti.Where(a => !Stornato(a)).ToList();

        var incassatiNelPeriodo = validi
            .Where(a => a.ImportoPagato > 0 && DataIncasso(a) >= dal && DataIncasso(a) <= al)
            .ToList();

        var inCorsoNelPeriodo = validi
            .Where(a => a.DataInizio.Date <= al && a.DataScadenza.Date >= dal)
            .ToList();

        var pagamentiMaestri = await context.PagamentiInsegnantis
            .AsNoTracking()
            .Where(p => p.Attivo == 1)
            .ToListAsync();

        var pagamentiNelPeriodo = pagamentiMaestri
            .Where(p => p.DataPagamento.Date >= dal && p.DataPagamento.Date <= al)
            .ToList();

        var primoAbbonamentoPerAllievo = validi
            .GroupBy(a => a.AllievoId)
            .Select(g => g.Min(a => a.DataInizio.Date));

        var riepilogo = new RiepilogoStatisticheDTO
        {
            Dal = dal,
            Al = al,
            Entrate = incassatiNelPeriodo.Sum(a => a.ImportoPagato),
            Uscite = pagamentiNelPeriodo.Sum(p => p.Importo),
            DaIncassare = validi
                .Where(a => a.DataInizio.Date >= dal && a.DataInizio.Date <= al)
                .Sum(a => a.DaPagare),
            AbbonamentiVenduti = incassatiNelPeriodo.Count,
            AllieviAttivi = inCorsoNelPeriodo.Select(a => a.AllievoId).Distinct().Count(),
            AllieviNuovi = primoAbbonamentoPerAllievo.Count(d => d >= dal && d <= al),
            AllieviDaAbbonare = await context.Allievis.CountAsync(a => a.Attivo == 1 && a.DaAbbonare == 1),
            EntratePerMese = CalcolaEntratePerMese(incassatiNelPeriodo, dal, al),
            Corsi = CalcolaCorsi(inCorsoNelPeriodo, incassatiNelPeriodo),
            TipiAbbonamento = CalcolaTipiAbbonamento(incassatiNelPeriodo),
            CampiExtra = await CalcolaCampiExtraAsync(context, dal, al)
        };

        var lezioni = await _dbService.GetLezioniRicorrentiAsync();

        riepilogo.Insegnanti = await CalcolaInsegnantiAsync(lezioni, pagamentiMaestri, dal, al);
        riepilogo.CompensiPrevisti = riepilogo.Insegnanti.Sum(i => i.CompensoPrevisto);

        (riepilogo.LezioniPiuFrequentate, riepilogo.LezioniMenoFrequentate) = CalcolaLezioni(lezioni, inCorsoNelPeriodo);

        return riepilogo;
    }

    private static List<VoceGraficoDTO> CalcolaEntratePerMese(List<Abbonamenti> incassati, DateTime dal, DateTime al)
    {
        var voci = new List<VoceGraficoDTO>();

        for (var mese = new DateTime(dal.Year, dal.Month, 1); mese <= al; mese = mese.AddMonths(1))
        {
            var delMese = incassati
                .Where(a => DataIncasso(a).Year == mese.Year && DataIncasso(a).Month == mese.Month)
                .ToList();

            double totale = delMese.Sum(a => a.ImportoPagato);

            voci.Add(new VoceGraficoDTO
            {
                Etichetta = Italiano.TextInfo.ToTitleCase(mese.ToString("MMM yyyy", Italiano)),
                Valore = totale,
                ValoreTesto = $"€ {totale:N2}",
                Dettaglio = delMese.Count == 1 ? "1 pagamento" : $"{delMese.Count} pagamenti",
                Colore = "#10B981"
            });
        }

        ImpostaProporzioni(voci);
        return voci;
    }

    private static List<VoceGraficoDTO> CalcolaCorsi(List<Abbonamenti> inCorso, List<Abbonamenti> incassati)
    {
        var voci = inCorso
            .GroupBy(a => a.CorsoId)
            .Select(g =>
            {
                var corso = g.First().Corso;
                int allievi = g.Select(a => a.AllievoId).Distinct().Count();
                double incasso = incassati.Where(a => a.CorsoId == g.Key).Sum(a => a.ImportoPagato);

                return new VoceGraficoDTO
                {
                    Etichetta = corso?.Nome ?? "Corso eliminato",
                    Valore = allievi,
                    ValoreTesto = allievi == 1 ? "1 allievo" : $"{allievi} allievi",
                    Dettaglio = $"€ {incasso:N2} incassati",
                    Colore = string.IsNullOrWhiteSpace(corso?.Colore) ? "#4F46E5" : corso!.Colore!
                };
            })
            .OrderByDescending(v => v.Valore)
            .ThenBy(v => v.Etichetta)
            .ToList();

        ImpostaProporzioni(voci);
        return voci;
    }

    private static List<VoceGraficoDTO> CalcolaTipiAbbonamento(List<Abbonamenti> incassati)
    {
        var voci = incassati
            .GroupBy(a => string.IsNullOrWhiteSpace(a.TipoAbbonamento) ? "Altro" : a.TipoAbbonamento)
            .Select(g => new VoceGraficoDTO
            {
                Etichetta = g.Key,
                Valore = g.Count(),
                ValoreTesto = g.Count() == 1 ? "1 venduto" : $"{g.Count()} venduti",
                Dettaglio = $"€ {g.Sum(a => a.ImportoPagato):N2}",
                Colore = "#0EA5E9"
            })
            .OrderByDescending(v => v.Valore)
            .ToList();

        ImpostaProporzioni(voci);
        return voci;
    }

    /// <summary>
    /// Iscritti per ogni lezione dell'orario settimanale. L'abbonamento e' per
    /// corso, non per lezione: gli iscritti di una lezione sono gli allievi con
    /// un abbonamento valido nel periodo per il corso di quella lezione.
    /// </summary>
    private static (List<VoceGraficoDTO> Piu, List<VoceGraficoDTO> Meno) CalcolaLezioni(
        List<Lezioni> lezioni, List<Abbonamenti> inCorso)
    {
        const int quante = 5;

        var iscrittiPerCorso = inCorso
            .GroupBy(a => a.CorsoId)
            .ToDictionary(g => g.Key, g => g.Select(a => a.AllievoId).Distinct().Count());

        var voci = lezioni
            .Where(l => l.Corso == null || l.Corso.Attivo == 1)
            .Select(l =>
            {
                int iscritti = iscrittiPerCorso.TryGetValue(l.CorsoId, out int n) ? n : 0;
                string giorno = DateHelper.GetNomeGiornoDaDb(l.GiornoSettimana, "?");
                if (giorno.Length > 3) giorno = giorno[..3];

                var dettagli = new List<string> { $"{l.OraInizio}-{l.OraFine}" };
                if (l.Sala != null) dettagli.Add(l.Sala.Nome);
                if (l.Insegnante != null) dettagli.Add(l.Insegnante.NomeCompleto);
                if (l.Sala?.Capienza is > 0) dettagli.Add($"{iscritti}/{l.Sala.Capienza} posti");

                return new VoceGraficoDTO
                {
                    Etichetta = $"{giorno} {l.OraInizio} · {l.Corso?.Nome ?? "Corso"}",
                    Valore = iscritti,
                    ValoreTesto = iscritti == 1 ? "1 iscritto" : $"{iscritti} iscritti",
                    Dettaglio = string.Join(" · ", dettagli),
                    Colore = string.IsNullOrWhiteSpace(l.Corso?.Colore) ? "#4F46E5" : l.Corso!.Colore!
                };
            })
            .ToList();

        // Barre in proporzione alla lezione piu' piena, cosi' i due elenchi si confrontano.
        ImpostaProporzioni(voci);

        var piu = voci
            .OrderByDescending(v => v.Valore)
            .ThenBy(v => v.Etichetta)
            .Take(quante)
            .ToList();

        // Con poche lezioni i due elenchi non devono ripetere le stesse righe.
        var meno = voci
            .Except(piu)
            .OrderBy(v => v.Valore)
            .ThenBy(v => v.Etichetta)
            .Take(quante)
            .ToList();

        return (piu, meno);
    }

    private async Task<List<VoceInsegnanteDTO>> CalcolaInsegnantiAsync(
        List<Lezioni> lezioni, List<PagamentiInsegnanti> pagamenti, DateTime dal, DateTime al)
    {
        var insegnanti = await _dbService.GetInsegnantiAttiviAsync();

        var voci = new List<VoceInsegnanteDTO>();

        foreach (var insegnante in insegnanti)
        {
            var sue = lezioni.Where(l => l.InsegnanteId == insegnante.Id).ToList();
            double ore = await _compensiService.CalcolaOrePeriodoAsync(sue, dal, al);
            // Il pagato si confronta con le ore del periodo, quindi conta il mese a
            // cui si riferisce (lo stipendio di settembre pagato il 5 ottobre e'
            // di settembre). Le "Uscite" in alto invece guardano il giorno del pagamento.
            double pagato = pagamenti
                .Where(p => p.InsegnanteId == insegnante.Id)
                .Where(p => (p.PeriodoDal ?? p.DataPagamento).Date >= dal && (p.PeriodoDal ?? p.DataPagamento).Date <= al)
                .Sum(p => p.Importo);

            // Maestro senza lezioni e senza pagamenti nel periodo: non serve in elenco.
            if (ore <= 0 && pagato <= 0) continue;

            double tariffa = insegnante.TariffaOraria ?? 0;

            voci.Add(new VoceInsegnanteDTO
            {
                Nome = insegnante.NomeCompleto,
                Tariffa = tariffa,
                Ore = ore,
                CompensoPrevisto = ore * tariffa,
                Pagato = pagato
            });
        }

        return voci.OrderByDescending(v => v.Ore).ToList();
    }

    private static async Task<List<GruppoCampoExtraDTO>> CalcolaCampiExtraAsync(PulseContext context, DateTime dal, DateTime al)
    {
        var risposte = await context.CampiExtraAllievos
            .AsNoTracking()
            .Where(c => c.Attivo == 1 && c.Valore != null && c.Valore != "")
            .ToListAsync();

        risposte = risposte
            .Where(c => c.DataInserimento == null || (c.DataInserimento.Value.Date >= dal && c.DataInserimento.Value.Date <= al))
            // "CONOSCENZA_ALTRO" e simili sono il dettaglio scritto a mano di una
            // risposta ALTRO: testo libero, non ha senso contarlo.
            .Where(c => !c.Chiave.EndsWith("_ALTRO", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Etichette e ordine delle domande come nel modello della scuola, se c'e'.
        List<CampoExtraDTO> definizioni;
        try { definizioni = PrivacyDocumentService.LeggiCampiExtraPerDispositivo(); }
        catch { definizioni = new(); }

        var gruppi = new List<GruppoCampoExtraDTO>();

        foreach (var perChiave in risposte.GroupBy(c => c.Chiave.ToUpperInvariant()))
        {
            var definizione = definizioni.FirstOrDefault(d => string.Equals(d.Chiave, perChiave.Key, StringComparison.OrdinalIgnoreCase));

            string etichetta = (string.IsNullOrWhiteSpace(definizione?.Etichetta) ? null : definizione!.Etichetta)
                ?? perChiave.OrderByDescending(c => c.DataInserimento).Select(c => c.Etichetta).FirstOrDefault(e => !string.IsNullOrWhiteSpace(e))
                ?? perChiave.Key;

            var conteggi = perChiave
                .GroupBy(c => c.Valore!.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => (Valore: g.Key, Conteggio: g.Count()))
                .OrderByDescending(x => x.Conteggio)
                .ThenBy(x => x.Valore)
                .ToList();

            int totale = conteggi.Sum(x => x.Conteggio);

            // Le risposte libere (es. professione) possono essere tantissime:
            // si mostrano le 10 piu' frequenti e il resto si somma in "Altre".
            var principali = conteggi.Take(10).ToList();
            int altre = conteggi.Skip(10).Sum(x => x.Conteggio);

            var voci = principali
                .Select(x => new VoceGraficoDTO
                {
                    Etichetta = x.Valore,
                    Valore = x.Conteggio,
                    ValoreTesto = $"{x.Conteggio} ({Percentuale(x.Conteggio, totale)})",
                    Colore = "#7C3AED"
                })
                .ToList();

            if (altre > 0)
            {
                voci.Add(new VoceGraficoDTO
                {
                    Etichetta = $"Altre risposte ({conteggi.Count - 10})",
                    Valore = altre,
                    ValoreTesto = $"{altre} ({Percentuale(altre, totale)})",
                    Colore = "#94A3B8"
                });
            }

            ImpostaProporzioni(voci);

            gruppi.Add(new GruppoCampoExtraDTO
            {
                Chiave = perChiave.Key,
                Etichetta = etichetta,
                TotaleRisposte = totale,
                Risposte = voci
            });
        }

        // Prima le domande nell'ordine del modello, poi le altre in ordine alfabetico.
        return gruppi
            .OrderBy(g =>
            {
                int indice = definizioni.FindIndex(d => string.Equals(d.Chiave, g.Chiave, StringComparison.OrdinalIgnoreCase));
                return indice < 0 ? int.MaxValue : indice;
            })
            .ThenBy(g => g.Etichetta)
            .ToList();
    }

    private static string Percentuale(int parte, int totale) =>
        totale == 0 ? "0%" : $"{(double)parte / totale:P0}";

    private static void ImpostaProporzioni(List<VoceGraficoDTO> voci)
    {
        double massimo = voci.Count == 0 ? 0 : voci.Max(v => v.Valore);
        foreach (var voce in voci)
        {
            voce.Proporzione = massimo > 0 ? Math.Clamp(voce.Valore / massimo, 0, 1) : 0;
        }
    }

    // ================================================
    // QUERY SALVATE
    // ================================================

    public async Task<List<QuerySalvate>> GetQuerySalvateAsync(bool includiSoloAmministratore)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.QuerySalvates
            .AsNoTracking()
            .Where(q => q.Attivo == 1 && (includiSoloAmministratore || q.SoloAmministratore != 1))
            .OrderBy(q => q.Ordine ?? int.MaxValue)
            .ThenBy(q => q.Nome)
            .ToListAsync();
    }

    public async Task<bool> SalvaQueryAsync(QuerySalvate query)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        bool nuova = query.Id == 0;
        if (nuova) query.Attivo = 1;

        context.Attach(query);
        context.Entry(query).State = nuova ? EntityState.Added : EntityState.Modified;
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<bool> EliminaQueryAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var query = await context.QuerySalvates.FindAsync(id);
        if (query == null) return false;

        query.Attivo = 0; // Soft delete
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<RisultatoQueryDTO> EseguiQueryAsync(string testoQuery, int queryId = 0, int maxRighe = 1000)
    {
        var risultato = new RisultatoQueryDTO();

        string? errore = ControllaQuery(testoQuery, out string sql);
        if (errore != null)
        {
            risultato.Errore = errore;
            return risultato;
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        // Connessione separata, aperta in sola lettura: e' SQLite stesso a
        // rifiutare qualsiasi INSERT/UPDATE/DELETE/DROP, qualunque cosa ci sia scritto.
        var costruttore = new SqliteConnectionStringBuilder(context.Database.GetConnectionString())
        {
            Mode = SqliteOpenMode.ReadOnly
        };

        try
        {
            await using var connessione = new SqliteConnection(costruttore.ToString());
            await connessione.OpenAsync();

            await using var comando = connessione.CreateCommand();
            comando.CommandText = sql;

            await using var lettore = await comando.ExecuteReaderAsync();

            for (int i = 0; i < lettore.FieldCount; i++)
            {
                risultato.Colonne.Add(lettore.GetName(i));
            }

            while (await lettore.ReadAsync())
            {
                if (risultato.Righe.Count >= maxRighe)
                {
                    risultato.Troncato = true;
                    break;
                }

                var riga = new string[lettore.FieldCount];
                for (int i = 0; i < lettore.FieldCount; i++)
                {
                    riga[i] = FormattaValore(lettore.IsDBNull(i) ? null : lettore.GetValue(i));
                }
                risultato.Righe.Add(riga);
            }
        }
        catch (Exception ex)
        {
            risultato.Errore = ex.Message;
            return risultato;
        }

        if (queryId > 0)
        {
            await context.QuerySalvates
                .Where(q => q.Id == queryId)
                .ExecuteUpdateAsync(s => s.SetProperty(q => q.UltimaEsecuzione, DateTime.Now));
        }

        return risultato;
    }

    /// <summary>
    /// Primo filtro, per dare un messaggio chiaro. La vera protezione e' la
    /// connessione in sola lettura.
    /// </summary>
    private static string? ControllaQuery(string? testo, out string sql)
    {
        sql = (testo ?? string.Empty).Trim().TrimEnd(';').Trim();

        if (sql.Length == 0)
            return "La query è vuota.";

        if (sql.Contains(';'))
            return "Scrivi una sola query per volta (senza ';' in mezzo).";

        string primaParola = sql.Split((char[]?)null, 2, StringSplitOptions.RemoveEmptyEntries)[0].ToUpperInvariant();
        if (primaParola != "SELECT" && primaParola != "WITH")
            return "Sono ammesse solo query di lettura, che iniziano con SELECT (o WITH).";

        return null;
    }

    private static string FormattaValore(object? valore) => valore switch
    {
        null => string.Empty,
        double d => d.ToString("0.##", Italiano),
        float f => f.ToString("0.##", Italiano),
        decimal m => m.ToString("0.##", Italiano),
        byte[] b => $"[{b.Length} byte]",
        _ => valore.ToString() ?? string.Empty
    };
}
