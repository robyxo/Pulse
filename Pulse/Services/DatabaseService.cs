using Microsoft.EntityFrameworkCore;
using Pulse.Models;

namespace Pulse.Services;

public class DatabaseService : IDatabaseService
{
    private readonly IDbContextFactory<PulseContext> _contextFactory;

    public DatabaseService(IDbContextFactory<PulseContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    // ================================================
    // AGGANCIO ENTITÀ AL CONTEXT
    // ================================================

    /// <summary>
    /// Aggancia l'entità al context senza trascinarsi dietro le proprietà di
    /// navigazione già valorizzate: le entità collegate che hanno già un Id
    /// restano "Unchanged", così EF non prova a reinserirle come righe nuove.
    /// Serve perché ogni chiamata usa un context nuovo, che non conosce le
    /// entità caricate in precedenza.
    /// </summary>
    private static void AgganciaPerSalvataggio<T>(PulseContext context, T entita, bool nuova) where T : class
    {
        context.Attach(entita);
        context.Entry(entita).State = nuova ? EntityState.Added : EntityState.Modified;
    }

    // ================================================
    // GESTIONE CORSI
    // ================================================

    public async Task<List<Corsi>> GetCorsiAttiviAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Corsis
            .Where(c => c.Attivo == 1)
            .OrderBy(c => c.Nome)
            .ToListAsync();
    }

    public async Task<bool> SalvaCorsoAsync(Corsi corso)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        bool nuovo = corso.Id == 0;
        if (nuovo) corso.Attivo = 1;

        AgganciaPerSalvataggio(context, corso, nuovo);
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<bool> EliminaCorsoAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var corso = await context.Corsis.FindAsync(id);
        if (corso == null) return false;

        corso.Attivo = 0; // Soft delete
        return await context.SaveChangesAsync() > 0;
    }

    // ================================================
    // CALENDARIO E LEZIONI
    // ================================================

    // Le lezioni sono ricorrenti settimanali (GiornoSettimana 1-7): sono le stesse
    // per qualunque settimana, quindi non c'è nessuna data da cui filtrare.
    public async Task<List<Lezioni>> GetLezioniRicorrentiAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Lezionis
            .Include(l => l.Corso)
            .Include(l => l.Insegnante)
            .Where(l => l.GiornoSettimana >= 1 && l.GiornoSettimana <= 7)
            .OrderBy(l => l.GiornoSettimana)
            .ThenBy(l => l.OraInizio)
            .ToListAsync();
    }

    public async Task<bool> SalvaLezioneAsync(Lezioni lezione)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        bool nuova = lezione.Id == 0;

        AgganciaPerSalvataggio(context, lezione, nuova);
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<bool> EliminaLezioneAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var lezione = await context.Lezionis.FindAsync(id);
        if (lezione == null) return false;

        context.Lezionis.Remove(lezione);
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<List<Lezioni>> GetLezioniPerCorsoAsync(int corsoId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Lezionis
            .Where(l => l.CorsoId == corsoId)
            .OrderBy(l => l.GiornoSettimana)
            .ThenBy(l => l.OraInizio)
            .ToListAsync();
    }

    // ================================================
    // MAESTRI
    // ================================================

    public async Task<List<Insegnanti>> GetInsegnantiAttiviAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Insegnantis
            .Where(i => i.Attivo == 1)
            .OrderBy(i => i.Cognome)
            .ThenBy(i => i.Nome)
            .ToListAsync();
    }

    public async Task<bool> SalvaInsegnanteAsync(Insegnanti insegnante)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        bool nuovo = insegnante.Id == 0;
        if (nuovo) insegnante.Attivo = 1;

        AgganciaPerSalvataggio(context, insegnante, nuovo);
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<bool> EliminaInsegnanteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var insegnante = await context.Insegnantis.FindAsync(id);
        if (insegnante == null) return false;

        insegnante.Attivo = 0; // Soft delete
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<List<Lezioni>> GetLezioniPerInsegnanteAsync(int insegnanteId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Lezionis
            .Include(l => l.Corso)
            .Where(l => l.InsegnanteId == insegnanteId)
            .OrderBy(l => l.GiornoSettimana)
            .ThenBy(l => l.OraInizio)
            .ToListAsync();
    }

    // ================================================
    // ALLIEVI
    // ================================================

    public async Task<List<Allievi>> GetAllieviAttiviAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Allievis
            .Where(a => a.Attivo == 1)
            .OrderBy(a => a.Cognome)
            .ThenBy(a => a.Nome)
            .ToListAsync();
    }

    public async Task<List<Allievi>> GetAllieviPerCorsoAsync(int corsoId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Mostra allievi che frequentano questo corso (anche con mese scaduto per permettere rinnovo)
        // Esclude chi è sospeso/in pausa o chi ha abbonamento cancellato (Attivo == 0)
        return await context.Abbonamentis
            .Where(a => a.CorsoId == corsoId
                     && a.Attivo == 1
                     && a.IsSospeso == 0)
            .Include(a => a.Allievo)
            .Select(a => a.Allievo!)
            .Where(allievo => allievo != null && allievo.Attivo == 1)
            .Distinct()
            .OrderBy(a => a.Cognome)
            .ThenBy(a => a.Nome)
            .ToListAsync();
    }

    public async Task<bool> SalvaAllievoAsync(Allievi allievo)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        bool nuovo = allievo.Id == 0;
        if (nuovo) allievo.Attivo = 1;

        AgganciaPerSalvataggio(context, allievo, nuovo);
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<bool> EliminaAllievoAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var allievo = await context.Allievis.FindAsync(id);
        if (allievo == null) return false;

        allievo.Attivo = 0; // Soft delete
        return await context.SaveChangesAsync() > 0;
    }

    // ================================================
    // ABBONAMENTI
    // ================================================

    // Tutti gli abbonamenti attivi in una sola query: serve a costruire la lista
    // allievi senza interrogare il database una volta per ogni riga.
    public async Task<List<Abbonamenti>> GetAbbonamentiAttiviAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Abbonamentis
            .Include(a => a.Corso)
            .Where(a => a.Attivo == 1)
            .OrderByDescending(a => a.DataScadenza)
            .ToListAsync();
    }

    public async Task<List<Abbonamenti>> GetAbbonamentiAllievoAsync(int allievoId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Abbonamentis
            .Include(a => a.Corso)
            .Where(a => a.AllievoId == allievoId && a.Attivo == 1)
            .OrderByDescending(a => a.DataInizio)
            .ToListAsync();
    }

    public async Task<List<Abbonamenti>> GetAbbonamentiAttiviPerCorsoAsync(int corsoId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Abbonamentis
            .Include(a => a.Corso)
            .Where(a => a.CorsoId == corsoId && a.Attivo == 1)
            .OrderByDescending(a => a.DataInizio)
            .ToListAsync();
    }
    public async Task<bool> SalvaAbbonamentoAsync(Abbonamenti abbonamento)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        bool nuovo = abbonamento.Id == 0;
        if (nuovo) abbonamento.Attivo = 1;

        AgganciaPerSalvataggio(context, abbonamento, nuovo);
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<bool> EliminaAbbonamentoAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var abb = await context.Abbonamentis.FindAsync(id);
        if (abb == null) return false;

        abb.Attivo = 0; // Soft delete
        return await context.SaveChangesAsync() > 0;
    }

    // ================================================
    // CALENDARIO CHIUSURE
    // ================================================

    public async Task<List<CalendarioChiusure>> GetChiusureAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.CalendarioChiusures
            .OrderBy(c => c.DataInizio)
            .ToListAsync();
    }

    public async Task<(bool Successo, int AbbonamentiEstesi)> SalvaChiusuraAsync(CalendarioChiusure chiusura)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        bool eraNuova = chiusura.Id == 0;

        AgganciaPerSalvataggio(context, chiusura, eraNuova);

        bool salvataggioOk = await context.SaveChangesAsync() > 0;
        int abbonamentiEstesi = 0;

        // Estensione automatica: solo per NUOVE "Chiusure" (mai per gli "Eventi",
        // e mai in automatico quando si modifica una chiusura già esistente,
        // per evitare di estendere più volte gli stessi abbonamenti).
        if (salvataggioOk
            && eraNuova
            && string.Equals(chiusura.Tipo, "Chiusura", StringComparison.OrdinalIgnoreCase)
            && DateTime.TryParse(chiusura.DataInizio, out var dataInizioChiusura)
            && DateTime.TryParse(chiusura.DataFine, out var dataFineChiusura))
        {
            int giorniChiusura = (dataFineChiusura.Date - dataInizioChiusura.Date).Days + 1;

            if (giorniChiusura > 0)
            {
                // Solo gli abbonamenti ATTUALMENTE ATTIVI il cui periodo si sovrappone
                // alla chiusura (già esistenti a DB in questo momento).
                var abbonamentiSovrapposti = await context.Abbonamentis
                    .Where(a => a.Attivo == 1
                        && a.DataInizio.Date <= dataFineChiusura.Date
                        && a.DataScadenza.Date >= dataInizioChiusura.Date)
                    .ToListAsync();

                foreach (var abbonamento in abbonamentiSovrapposti)
                {
                    abbonamento.DataScadenza = abbonamento.DataScadenza.AddDays(giorniChiusura);
                }

                if (abbonamentiSovrapposti.Count > 0)
                {
                    await context.SaveChangesAsync();
                    abbonamentiEstesi = abbonamentiSovrapposti.Count;
                }
            }
        }

        return (salvataggioOk, abbonamentiEstesi);
    }

    public async Task<bool> EliminaChiusuraAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var chiusura = await context.CalendarioChiusures.FindAsync(id);
        if (chiusura == null) return false;

        context.CalendarioChiusures.Remove(chiusura);
        return await context.SaveChangesAsync() > 0;
    }

    // ================================================
    // PRIVACY
    // ================================================

    public async Task<List<Privacy>> GetPrivacyAllievoAsync(int allievoId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Privacies
            .Where(p => p.IdAllievo == allievoId)
            .OrderByDescending(p => p.Data)
            .ToListAsync();
    }

    public async Task<bool> SalvaPrivacyAsync(Privacy privacy)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        bool nuova = privacy.Id == 0;

        AgganciaPerSalvataggio(context, privacy, nuova);
        return await context.SaveChangesAsync() > 0;
    }
}