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

    public async Task<bool> EliminaLezioneAsync(int id)
    {
        var lezione = await _context.Lezionis.FindAsync(id);
        if (lezione == null) return false;

        _context.Lezionis.Remove(lezione);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<List<Lezioni>> GetLezioniPerCorsoAsync(int corsoId)
    {
        return await _context.Lezionis
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
        // Mostra allievi che frequentano questo corso (anche con mese scaduto per permettere rinnovo)
        // Esclude chi è sospeso/in pausa o chi ha abbonamento cancellato (Attivo == 0)
        return await _context.Abbonamentis
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

    public Task<List<Allievi>> GetAllieviPerCorso(int corsoId) => GetAllieviPerCorsoAsync(corsoId);

    public async Task<List<Allievi>> GetAllieviPerLezioneAsync(int lezioneId)
    {
        var lezione = await _context.Lezionis.FindAsync(lezioneId);
        if (lezione == null) return new List<Allievi>();

        return await GetAllieviPerCorsoAsync(lezione.CorsoId);
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

        allievo.Attivo = 0; // Soft delete
        _context.Allievis.Update(allievo);
        return await _context.SaveChangesAsync() > 0;
    }

    // ================================================
    // ABBONAMENTI
    // ================================================

    public async Task<List<Abbonamenti>> GetAbbonamentiAllievoAsync(int allievoId)
    {
        return await _context.Abbonamentis
            .Include(a => a.Corso)
            .Where(a => a.AllievoId == allievoId && a.Attivo == 1)
            .OrderByDescending(a => a.DataInizio)
            .ToListAsync();
    }

    public async Task<bool> SalvaAbbonamentoAsync(Abbonamenti abbonamento)
    {
        if (abbonamento.Id == 0)
        {
            abbonamento.Attivo = 1;
            _context.Abbonamentis.Add(abbonamento);
        }
        else
        {
            _context.Abbonamentis.Update(abbonamento);
        }

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> EliminaAbbonamentoAsync(int id)
    {
        var abb = await _context.Abbonamentis.FindAsync(id);
        if (abb == null) return false;

        abb.Attivo = 0; // Soft delete
        _context.Abbonamentis.Update(abb);
        return await _context.SaveChangesAsync() > 0;
    }

    // ================================================
    // CALENDARIO CHIUSURE
    // ================================================

    public async Task<List<CalendarioChiusure>> GetChiusureAsync()
    {
        return await _context.CalendarioChiusures
            .OrderBy(c => c.DataInizio)
            .ToListAsync();
    }

    public async Task<(bool Successo, int AbbonamentiEstesi)> SalvaChiusuraAsync(CalendarioChiusure chiusura)
    {
        bool eraNuova = chiusura.Id == 0;

        if (chiusura.Id == 0)
        {
            _context.CalendarioChiusures.Add(chiusura);
        }
        else
        {
            _context.CalendarioChiusures.Update(chiusura);
        }

        bool salvataggioOk = await _context.SaveChangesAsync() > 0;
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
                var abbonamentiSovrapposti = await _context.Abbonamentis
                    .Where(a => a.Attivo == 1
                        && a.DataInizio.Date <= dataFineChiusura.Date
                        && a.DataScadenza.Date >= dataInizioChiusura.Date)
                    .ToListAsync();

                foreach (var abbonamento in abbonamentiSovrapposti)
                {
                    abbonamento.DataScadenza = abbonamento.DataScadenza.AddDays(giorniChiusura);
                    _context.Abbonamentis.Update(abbonamento);
                }

                if (abbonamentiSovrapposti.Count > 0)
                {
                    await _context.SaveChangesAsync();
                    abbonamentiEstesi = abbonamentiSovrapposti.Count;
                }
            }
        }

        return (salvataggioOk, abbonamentiEstesi);
    }

    public async Task<bool> EliminaChiusuraAsync(int id)
    {
        var chiusura = await _context.CalendarioChiusures.FindAsync(id);
        if (chiusura == null) return false;

        _context.CalendarioChiusures.Remove(chiusura);
        return await _context.SaveChangesAsync() > 0;
    }

    // ================================================
    // PRIVACY
    // ================================================

    public async Task<List<Privacy>> GetPrivacyAllievoAsync(int allievoId)
    {
        return await _context.Privacies
            .Where(p => p.IdAllievo == allievoId)
            .OrderByDescending(p => p.Data)
            .ToListAsync();
    }

    public async Task<bool> SalvaPrivacyAsync(Privacy privacy)
    {
        if (privacy.Id == 0)
        {
            _context.Privacies.Add(privacy);
        }
        else
        {
            _context.Privacies.Update(privacy);
        }

        return await _context.SaveChangesAsync() > 0;
    }
}