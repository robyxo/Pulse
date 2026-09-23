using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Pulse.Helpers;
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
            .Include(l => l.Sala)
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
    // SALE
    // ================================================

    public async Task<List<Sale>> GetSaleAttiveAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Sales
            .Where(s => s.Attivo == 1)
            .OrderBy(s => s.Nome)
            .ToListAsync();
    }

    public async Task<bool> SalvaSalaAsync(Sale sala)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        bool nuova = sala.Id == 0;
        if (nuova) sala.Attivo = 1;

        AgganciaPerSalvataggio(context, sala, nuova);
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<bool> EliminaSalaAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var sala = await context.Sales.FindAsync(id);
        if (sala == null) return false;

        sala.Attivo = 0; // Soft delete
        return await context.SaveChangesAsync() > 0;
    }

    // Le lezioni sono ricorrenti settimanali, quindi per trovare le candidate
    // bastano sala e giorno: la sovrapposizione oraria si verifica poi in memoria.
    public async Task<List<Lezioni>> GetLezioniPerSalaEGiornoAsync(int salaId, int giornoSettimana, int lezioneDaEscludereId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Lezionis
            .Include(l => l.Corso)
            .Include(l => l.Insegnante)
            .Where(l => l.SalaId == salaId
                     && l.GiornoSettimana == giornoSettimana
                     && l.Id != lezioneDaEscludereId)
            .OrderBy(l => l.OraInizio)
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

    public async Task<Allievi?> GetAllievoAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Allievis.FirstOrDefaultAsync(a => a.Id == id && a.Attivo == 1);
    }

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
        bool salvato = await context.SaveChangesAsync() > 0;

        // Primo abbonamento di un allievo registrato (per esempio dal tablet):
        // non è più "da abbonare". Sta qui e non nei ViewModel così vale da ogni
        // punto: scheda allievo, rinnovo, pagamento rapido dal calendario.
        if (salvato && nuovo)
        {
            await context.Allievis
                .Where(a => a.Id == abbonamento.AllievoId && a.DaAbbonare == 1)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.DaAbbonare, 0));

            // Allinea anche la copia in memoria: altrimenti un "Salva allievo" fatto
            // subito dopo riscriverebbe DaAbbonare = 1 sul database.
            if (abbonamento.Allievo is { } allievo) allievo.DaAbbonare = 0;
        }

        return salvato;
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

    public async Task<(bool Successo, int AbbonamentiEstesi, int AbbonamentiChiusi)> SalvaChiusuraAsync(CalendarioChiusure chiusura)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        bool eraNuova = chiusura.Id == 0;

        AgganciaPerSalvataggio(context, chiusura, eraNuova);

        bool salvataggioOk = await context.SaveChangesAsync() > 0;
        int abbonamentiEstesi = 0;
        int abbonamentiChiusi = 0;

        // Gli abbonamenti si toccano solo alla creazione di una nuova chiusura:
        // mai per gli "Eventi", e mai rimodificando una chiusura esistente,
        // altrimenti gli stessi abbonamenti verrebbero spostati piu' volte.
        if (salvataggioOk
            && eraNuova
            && !string.Equals(chiusura.Tipo, TipiEvento.Evento, StringComparison.OrdinalIgnoreCase)
            && DateTime.TryParse(chiusura.DataInizio, out var dataInizioChiusura)
            && DateTime.TryParse(chiusura.DataFine, out var dataFineChiusura))
        {
            // Solo gli abbonamenti ATTUALMENTE ATTIVI il cui periodo si sovrappone
            // alla chiusura (gia' esistenti a DB in questo momento).
            var abbonamentiSovrapposti = await context.Abbonamentis
                .Where(a => a.Attivo == 1
                    && a.DataInizio.Date <= dataFineChiusura.Date
                    && a.DataScadenza.Date >= dataInizioChiusura.Date)
                .ToListAsync();

            bool stagionale = string.Equals(chiusura.Tipo, TipiEvento.ChiusuraStagionale, StringComparison.OrdinalIgnoreCase);

            if (stagionale)
            {
                // Fine stagione: gli abbonamenti si chiudono il giorno in cui chiude
                // la scuola, non vengono prolungati. Chi scade gia' prima resta com'e'.
                foreach (var abbonamento in abbonamentiSovrapposti)
                {
                    if (abbonamento.DataScadenza.Date > dataInizioChiusura.Date)
                    {
                        abbonamento.DataScadenza = dataInizioChiusura.Date;
                        abbonamentiChiusi++;
                    }
                }
            }
            else if (chiusura.Recupero != 0)
            {
                // Recupero attivo (impostazione predefinita): la chiusura viene
                // restituita agli allievi come giorni in piu' sull'abbonamento.
                int giorniChiusura = (dataFineChiusura.Date - dataInizioChiusura.Date).Days + 1;

                if (giorniChiusura > 0 && abbonamentiSovrapposti.Count > 0)
                {
                    foreach (var abbonamento in abbonamentiSovrapposti)
                    {
                        abbonamento.DataScadenza = abbonamento.DataScadenza.AddDays(giorniChiusura);
                    }

                    abbonamentiEstesi = abbonamentiSovrapposti.Count;
                }
            }

            if (abbonamentiEstesi > 0 || abbonamentiChiusi > 0)
            {
                await context.SaveChangesAsync();
            }
        }

        return (salvataggioOk, abbonamentiEstesi, abbonamentiChiusi);
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
    // COMUNICAZIONI INVIATE
    // ================================================

    public async Task<List<Comunicazioni>> GetComunicazioniAsync(int limite = 200)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Comunicazionis
            .Include(c => c.Allievo)
            .Where(c => c.Attivo == 1)
            .OrderByDescending(c => c.DataInvio)
            .Take(limite)
            .ToListAsync();
    }

    public async Task<bool> RegistraComunicazioneAsync(
        string tipo,
        string? oggetto,
        string? corpo,
        IEnumerable<string> destinatari,
        bool esito,
        string? messaggioErrore = null,
        int? allievoId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var elenco = destinatari?
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Select(d => d.Trim())
            .ToList() ?? new List<string>();

        var comunicazione = new Comunicazioni
        {
            DataInvio = DateTime.Now,
            Tipo = tipo,
            Oggetto = oggetto,
            Corpo = corpo,
            AllievoId = allievoId,
            // Un invio = una riga; gli indirizzi restano consultabili qui dentro.
            Destinatario = string.Join("; ", elenco),
            Esito = esito ? 1 : 0,
            MessaggioErrore = messaggioErrore,
            Attivo = 1
        };

        context.Comunicazionis.Add(comunicazione);
        return await context.SaveChangesAsync() > 0;
    }

    // ================================================
    // CAMPI EXTRA DEL MODULO
    // ================================================

    public async Task<List<CampiExtraAllievo>> GetCampiExtraAllievoAsync(int allievoId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.CampiExtraAllievos
            .Where(c => c.AllievoId == allievoId && c.Attivo == 1)
            .OrderBy(c => c.Chiave)
            .ToListAsync();
    }

    public async Task<bool> SalvaCampoExtraAsync(int allievoId, string chiave, string? etichetta, string? valore, string origine = "Tablet")
    {
        if (allievoId <= 0 || string.IsNullOrWhiteSpace(chiave)) return false;

        await using var context = await _contextFactory.CreateDbContextAsync();

        var esistente = await context.CampiExtraAllievos
            .FirstOrDefaultAsync(c => c.AllievoId == allievoId && c.Chiave == chiave);

        if (esistente == null)
        {
            context.CampiExtraAllievos.Add(new CampiExtraAllievo
            {
                AllievoId = allievoId,
                Chiave = chiave,
                Etichetta = etichetta,
                Valore = valore,
                DataInserimento = DateTime.Now,
                Origine = origine,
                Attivo = 1
            });
        }
        else
        {
            esistente.Valore = valore;
            esistente.Etichetta = etichetta ?? esistente.Etichetta;
            esistente.DataInserimento = DateTime.Now;
            esistente.Origine = origine;
            esistente.Attivo = 1;
        }

        return await context.SaveChangesAsync() > 0;
    }

    public async Task<List<(string Valore, int Conteggio)>> GetStatisticheCampoExtraAsync(string chiave)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var righe = await context.CampiExtraAllievos
            .Where(c => c.Chiave == chiave && c.Attivo == 1 && c.Valore != null && c.Valore != "")
            .GroupBy(c => c.Valore!)
            .Select(g => new { Valore = g.Key, Conteggio = g.Count() })
            .OrderByDescending(x => x.Conteggio)
            .ToListAsync();

        return righe.Select(r => (r.Valore, r.Conteggio)).ToList();
    }

    // ================================================
    // REGISTRAZIONE DA DISPOSITIVO (TABLET)
    // ================================================

    // Nome della riga in DispositiviAutorizzati che contiene la chiave del QR.
    private const string NomeDispositivoRegistrazioni = "Tablet registrazioni";

    public async Task<bool> EsisteCodiceFiscaleAsync(string codiceFiscale)
    {
        if (string.IsNullOrWhiteSpace(codiceFiscale)) return false;

        string cf = codiceFiscale.Trim().ToUpperInvariant();

        await using var context = await _contextFactory.CreateDbContextAsync();

        // Volutamente senza filtro su Attivo: anche un allievo eliminato conta,
        // se ne occupa la segreteria.
        return await context.Allievis
            .AnyAsync(a => a.CodiceFiscale != null && a.CodiceFiscale.Trim().ToUpper() == cf);
    }

    public async Task<string> GetOCreaChiaveDispositivoAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var esistente = await context.DispositiviAutorizzatis
            .Where(d => d.Nome == NomeDispositivoRegistrazioni && d.Attivo == 1)
            .OrderByDescending(d => d.Id)
            .FirstOrDefaultAsync();

        if (esistente != null) return esistente.Token;

        var nuovo = new DispositiviAutorizzati
        {
            Nome = NomeDispositivoRegistrazioni,
            Token = GeneraChiave(),
            Attivo = 1
        };
        context.DispositiviAutorizzatis.Add(nuovo);
        await context.SaveChangesAsync();

        return nuovo.Token;
    }

    public async Task<string> RigeneraChiaveDispositivoAsync()
    {
        await using (var context = await _contextFactory.CreateDbContextAsync())
        {
            await context.DispositiviAutorizzatis
                .Where(d => d.Nome == NomeDispositivoRegistrazioni && d.Attivo == 1)
                .ExecuteUpdateAsync(s => s.SetProperty(d => d.Attivo, 0));
        }

        return await GetOCreaChiaveDispositivoAsync();
    }

    public async Task<bool> VerificaChiaveDispositivoAsync(string? chiave, string? indirizzoIp)
    {
        if (string.IsNullOrWhiteSpace(chiave)) return false;

        await using var context = await _contextFactory.CreateDbContextAsync();

        var dispositivo = await context.DispositiviAutorizzatis
            .FirstOrDefaultAsync(d => d.Token == chiave && d.Attivo == 1);

        if (dispositivo == null) return false;

        dispositivo.UltimoAccesso = DateTime.Now;
        dispositivo.IndirizzoIp = indirizzoIp;
        await context.SaveChangesAsync();

        return true;
    }

    /// <summary>24 caratteri casuali, sicuri da mettere in un link.</summary>
    private static string GeneraChiave()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(18))
            .Replace('+', '-')
            .Replace('/', '_');
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