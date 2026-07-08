using Microsoft.EntityFrameworkCore;
using Pulse.Models;

namespace Pulse.Services;

public interface IDatabaseService
{
    // Lezioni
    Task<List<Lezioni>> GetLezioniSettimana(DateTime dataRiferimento);
    Task<Lezioni?> GetLezioneById(int id);

    // Corsi
    Task<List<Corsi>> GetAllCorsi();
    Task<Corsi?> GetCorsoById(int id);
    Task<List<Corsi>> GetCorsiAttivi();

    // Allievi
    Task<List<Allievi>> GetAllAllievi();
    Task<Allievi?> GetAllievoById(int id);
    Task<List<Allievi>> GetAllieviPerCorso(int corsoId);
    Task<List<Allievi>> GetAllieviPerLezione(int lezioneId);

    // Insegnanti
    Task<List<Insegnanti>> GetAllInsegnanti();
    Task<Insegnanti?> GetInsegnanteById(int id);

    // Iscrizioni
    Task<List<Iscrizioni>> GetIscrizioniPerCorso(int corsoId);
    Task<List<Iscrizioni>> GetIscrizioniPerAllievo(int allievoId);
    Task<bool> IsAllievoIscritto(int allievoId, int corsoId);

    // Presenze
    Task<List<Presenze>> GetPresenzePerLezione(int lezioneId);
    Task<List<Presenze>> GetPresenzePerAllievo(int allievoId);

    // Operazioni CRUD (per il futuro)
    Task<Corsi> AddCorso(Corsi corso);
    Task<Allievi> AddAllievo(Allievi allievo);
    Task<Lezioni> AddLezione(Lezioni lezione);
    Task<Iscrizioni> AddIscrizione(Iscrizioni iscrizione);

    Task UpdateCorso(Corsi corso);
    Task UpdateAllievo(Allievi allievo);
    Task UpdateLezione(Lezioni lezione);

    Task DeleteCorso(int id);
    Task DeleteAllievo(int id);
    Task DeleteLezione(int id);
    Task DeleteIscrizione(int id);
}

public class DatabaseService : IDatabaseService
{
    private readonly PulseContext _context;

    public DatabaseService(PulseContext context)
    {
        _context = context;
    }

    // ================================================
    // LEZIONI
    // ================================================

    public async Task<List<Lezioni>> GetLezioniSettimana(DateTime dataRiferimento)
    {
        // Calcola l'inizio della settimana (Lunedì)
        var inizioSettimana = dataRiferimento.Date;
        while (inizioSettimana.DayOfWeek != DayOfWeek.Monday)
        {
            inizioSettimana = inizioSettimana.AddDays(-1);
        }

        // Calcola la fine della settimana (Domenica)
        var fineSettimana = inizioSettimana.AddDays(7);

        // Recupera le lezioni con i corsi e gli insegnanti
        return await _context.Lezionis
            .Include(l => l.Corso)
            .Include(l => l.Insegnante)
            .Where(l => l.GiornoSettimana >= (int)DayOfWeek.Monday &&
                        l.GiornoSettimana <= (int)DayOfWeek.Sunday)
            .OrderBy(l => l.GiornoSettimana)
            .ThenBy(l => l.OraInizio)
            .ToListAsync();
    }

    public async Task<Lezioni?> GetLezioneById(int id)
    {
        return await _context.Lezionis
            .Include(l => l.Corso)
            .Include(l => l.Insegnante)
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    // ================================================
    // CORSI
    // ================================================

    public async Task<List<Corsi>> GetAllCorsi()
    {
        return await _context.Corsis
            .OrderBy(c => c.Nome)
            .ToListAsync();
    }

    public async Task<Corsi?> GetCorsoById(int id)
    {
        return await _context.Corsis
            .Include(c => c.Lezionis)
            .Include(c => c.Iscrizionis)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<Corsi>> GetCorsiAttivi()
    {
        return await _context.Corsis
            .Where(c => c.Attivo == 1)
            .OrderBy(c => c.Nome)
            .ToListAsync();
    }

    // ================================================
    // ALLIEVI
    // ================================================

    public async Task<List<Allievi>> GetAllAllievi()
    {
        return await _context.Allievis
            .Where(a => a.Attivo == 1)
            .OrderBy(a => a.Cognome)
            .ThenBy(a => a.Nome)
            .ToListAsync();
    }

    public async Task<Allievi?> GetAllievoById(int id)
    {
        return await _context.Allievis
            .Include(a => a.Iscrizionis)
            .ThenInclude(i => i.Corso)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<List<Allievi>> GetAllieviPerCorso(int corsoId)
    {
        return await _context.Iscrizionis
            .Where(i => i.CorsoId == corsoId && i.Attivo == 1)
            .Include(i => i.Allievo)
            .Select(i => i.Allievo!)
            .OrderBy(a => a.Cognome)
            .ThenBy(a => a.Nome)
            .ToListAsync();
    }

    public async Task<List<Allievi>> GetAllieviPerLezione(int lezioneId)
    {
        var lezione = await _context.Lezionis
            .Include(l => l.Corso)
            .ThenInclude(c => c.Iscrizionis)
            .ThenInclude(i => i.Allievo)
            .FirstOrDefaultAsync(l => l.Id == lezioneId);

        if (lezione == null) return new List<Allievi>();

        return lezione.Corso?.Iscrizionis   
            .Where(i => i.Attivo == 1)
            .Select(i => i.Allievo!)
            .OrderBy(a => a.Cognome)
            .ThenBy(a => a.Nome)
            .ToList() ?? new List<Allievi>();
    }

    // ================================================
    // INSEGNANTI
    // ================================================

    public async Task<List<Insegnanti>> GetAllInsegnanti()
    {
        return await _context.Insegnantis
            .Where(i => i.Attivo == 1)
            .OrderBy(i => i.Cognome)
            .ThenBy(i => i.Nome)
            .ToListAsync();
    }

    public async Task<Insegnanti?> GetInsegnanteById(int id)
    {
        return await _context.Insegnantis
            .Include(i => i.Lezionis)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    // ================================================
    // ISCRIZIONI
    // ================================================

    public async Task<List<Iscrizioni>> GetIscrizioniPerCorso(int corsoId)
    {
        return await _context.Iscrizionis
            .Where(i => i.CorsoId == corsoId && i.Attivo == 1)
            .Include(i => i.Allievo)
            .ToListAsync();
    }

    public async Task<List<Iscrizioni>> GetIscrizioniPerAllievo(int allievoId)
    {
        return await _context.Iscrizionis
            .Where(i => i.AllievoId == allievoId && i.Attivo == 1)
            .Include(i => i.Corso)
            .ToListAsync();
    }

    public async Task<bool> IsAllievoIscritto(int allievoId, int corsoId)
    {
        return await _context.Iscrizionis
            .AnyAsync(i => i.AllievoId == allievoId &&
                          i.CorsoId == corsoId &&
                          i.Attivo == 1);
    }

    // ================================================
    // PRESENZE
    // ================================================

    public async Task<List<Presenze>> GetPresenzePerLezione(int lezioneId)
    {
        return await _context.Presenzes
            .Where(p => p.LezioneId == lezioneId)
            .Include(p => p.Allievo)
            .ToListAsync();
    }

    public async Task<List<Presenze>> GetPresenzePerAllievo(int allievoId)
    {
        return await _context.Presenzes
            .Where(p => p.AllievoId == allievoId)
            .Include(p => p.Lezione)
            .ThenInclude(l => l.Corso)
            .OrderByDescending(p => p.Data)
            .ToListAsync();
    }

    // ================================================
    // OPERAZIONI CRUD
    // ================================================

    public async Task<Corsi> AddCorso(Corsi corso)
    {
        _context.Corsis.Add(corso);
        await _context.SaveChangesAsync();
        return corso;
    }

    public async Task<Allievi> AddAllievo(Allievi allievo)
    {
        allievo.DataIscrizione = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        _context.Allievis.Add(allievo);
        await _context.SaveChangesAsync();
        return allievo;
    }

    public async Task<Lezioni> AddLezione(Lezioni lezione)
    {
        _context.Lezionis.Add(lezione);
        await _context.SaveChangesAsync();
        return lezione;
    }

    public async Task<Iscrizioni> AddIscrizione(Iscrizioni iscrizione)
    {
        iscrizione.DataIscrizione = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        _context.Iscrizionis.Add(iscrizione);
        await _context.SaveChangesAsync();
        return iscrizione;
    }

    public async Task UpdateCorso(Corsi corso)
    {
        _context.Entry(corso).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAllievo(Allievi allievo)
    {
        _context.Entry(allievo).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task UpdateLezione(Lezioni lezione)
    {
        _context.Entry(lezione).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteCorso(int id)
    {
        var corso = await _context.Corsis.FindAsync(id);
        if (corso != null)
        {
            _context.Corsis.Remove(corso);
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteAllievo(int id)
    {
        var allievo = await _context.Allievis.FindAsync(id);
        if (allievo != null)
        {
            _context.Allievis.Remove(allievo);
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteLezione(int id)
    {
        var lezione = await _context.Lezionis.FindAsync(id);
        if (lezione != null)
        {
            _context.Lezionis.Remove(lezione);
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteIscrizione(int id)
    {
        var iscrizione = await _context.Iscrizionis.FindAsync(id);
        if (iscrizione != null)
        {
            _context.Iscrizionis.Remove(iscrizione);
            await _context.SaveChangesAsync();
        }
    }
}