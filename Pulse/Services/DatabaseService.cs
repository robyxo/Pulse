using Microsoft.EntityFrameworkCore;
using Pulse.Models;

namespace Pulse.Services;

public class DatabaseService : IDatabaseService
{
    private readonly PulseContext _context;

    public DatabaseService(PulseContext context)
    {
        _context = context;
    }

    // ================================================
    // GESTIONE CORSI
    // ================================================

    public async Task<List<Corsi>> GetCorsiAttiviAsync()
    {
        return await _context.Corsis
            .Where(c => c.Attivo == 1)
            .OrderBy(c => c.Nome)
            .ToListAsync();
    }

    public Task<List<Corsi>> GetCorsiAttivi() => GetCorsiAttiviAsync();

    public async Task<bool> SalvaCorsoAsync(Corsi corso)
    {
        if (corso.Id == 0)
        {
            corso.Attivo = 1;
            _context.Corsis.Add(corso);
        }
        else
        {
            _context.Corsis.Update(corso);
        }

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> EliminaCorsoAsync(int id)
    {
        var corso = await _context.Corsis.FindAsync(id);
        if (corso == null) return false;

        corso.Attivo = 0;
        _context.Corsis.Update(corso);
        return await _context.SaveChangesAsync() > 0;
    }

    public Task<bool> EliminaCorsoAsync(Corsi corso) => EliminaCorsoAsync(corso.Id);

    // ================================================
    // CALENDARIO E LEZIONI
    // ================================================

    public async Task<List<Lezioni>> GetLezioniSettimanaAsync(DateTime dataRiferimento)
    {
        return await _context.Lezionis
            .Include(l => l.Corso)
                .ThenInclude(c => c.Iscrizionis)
            .Include(l => l.Insegnante)
            .Where(l => l.GiornoSettimana >= 1 && l.GiornoSettimana <= 7)
            .OrderBy(l => l.GiornoSettimana)
            .ThenBy(l => l.OraInizio)
            .ToListAsync();
    }

    public Task<List<Lezioni>> GetLezioniSettimana(DateTime dataRiferimento) => 
        GetLezioniSettimanaAsync(dataRiferimento);

    public async Task<bool> SalvaLezioneAsync(Lezioni lezione)
    {
        if (lezione.Id == 0)
        {
            _context.Lezionis.Add(lezione);
        }
        else
        {
            _context.Lezionis.Update(lezione);
        }

        return await _context.SaveChangesAsync() > 0;
    }

    public Task<bool> SalvaLezione(Lezioni lezione) => 
        SalvaLezioneAsync(lezione);

    // ================================================
    // MAESTRI
    // ================================================

    public async Task<List<Insegnanti>> GetInsegnantiAttiviAsync()
    {
        return await _context.Insegnantis
            .Where(i => i.Attivo == 1)
            .OrderBy(i => i.Cognome)
            .ThenBy(i => i.Nome)
            .ToListAsync();
    }
    public async Task<bool> SalvaInsegnanteAsync(Insegnanti insegnante)
    {
        if (insegnante.Id == 0)
        {
            insegnante.Attivo = 1;
            _context.Insegnantis.Add(insegnante);
        }
        else
        {
            _context.Insegnantis.Update(insegnante);
        }

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> EliminaInsegnanteAsync(int id)
    {
        var insegnante = await _context.Insegnantis.FindAsync(id);
        if (insegnante == null) return false;

        insegnante.Attivo = 0; // Soft delete
        _context.Insegnantis.Update(insegnante);
        return await _context.SaveChangesAsync() > 0;
    }

    public Task<List<Insegnanti>> GetInsegnantiAttivi() => GetInsegnantiAttiviAsync();

    // ================================================
    // ALLIEVI
    // ================================================

    public async Task<List<Allievi>> GetAllieviAttiviAsync()
    {
        return await _context.Allievis
            .Where(a => a.Attivo == 1)
            .OrderBy(a => a.Cognome)
            .ThenBy(a => a.Nome)
            .ToListAsync();
    }

    public async Task<List<Allievi>> GetAllieviPerCorsoAsync(int corsoId)
    {
        return await _context.Iscrizionis
            .Where(i => i.CorsoId == corsoId && i.Attivo == 1)
            .Include(i => i.Allievo)
            .Select(i => i.Allievo!)
            .Where(a => a != null && a.Attivo == 1)
            .OrderBy(a => a.Cognome)
            .ThenBy(a => a.Nome)
            .ToListAsync();
    }
    public async Task<bool> SalvaAllievoAsync(Allievi allievo)
    {
        if (allievo.Id == 0)
        {
            allievo.Attivo = 1;
            _context.Allievis.Add(allievo);
        }
        else
        {
            _context.Allievis.Update(allievo);
        }

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> EliminaAllievoAsync(int id)
    {
        var allievo = await _context.Allievis.FindAsync(id);
        if (allievo == null) return false;

        allievo.Attivo = 0; // Soft delete per sicurezza storica
        _context.Allievis.Update(allievo);
        return await _context.SaveChangesAsync() > 0;
    }

    public Task<List<Allievi>> GetAllieviPerCorso(int corsoId) => GetAllieviPerCorsoAsync(corsoId);

    public async Task<List<Allievi>> GetAllieviPerLezioneAsync(int lezioneId)
    {
        var lezione = await _context.Lezionis
            .Include(l => l.Corso)
                .ThenInclude(c => c.Iscrizionis)
                    .ThenInclude(i => i.Allievo)
            .FirstOrDefaultAsync(l => l.Id == lezioneId);

        if (lezione?.Corso == null) return new List<Allievi>();

        return lezione.Corso.Iscrizionis
            .Where(i => i.Attivo == 1 && i.Allievo != null && i.Allievo.Attivo == 1)
            .Select(i => i.Allievo!)
            .OrderBy(a => a.Cognome)
            .ThenBy(a => a.Nome)
            .ToList();
    }
}