using Pulse.Models;

namespace Pulse.DataBase;

public static class DbSeeder
{
    public static async Task SeedAsync(PulseContext db)
    {
        if (db.Corsis.Any()) return; // se ci sono già dati, esci

        // 1. Insegnanti
        var prof1 = new Insegnanti { Nome = "Marco", Cognome = "Rossi", Attivo = 1 };
        var prof2 = new Insegnanti { Nome = "Laura", Cognome = "Bianchi", Attivo = 1 };
        db.Insegnantis.AddRange(prof1, prof2);
        await db.SaveChangesAsync();

        // 2. Corsi
        var corso1 = new Corsi { Nome = "Pianoforte", Colore = "#4F46E5", Attivo = 1 };
        var corso2 = new Corsi { Nome = "Chitarra", Colore = "#10B981", Attivo = 1 };
        var corso3 = new Corsi { Nome = "Batteria", Colore = "#F59E0B", Attivo = 1 };
        db.Corsis.AddRange(corso1, corso2, corso3);
        await db.SaveChangesAsync();

        // 3. Allievi
        var allievi = new List<Allievi>
        {
            new() { Nome = "Luca", Cognome = "Verdi", Attivo = 1 },
            new() { Nome = "Sara", Cognome = "Neri", Attivo = 1 },
            new() { Nome = "Paolo", Cognome = "Gialli", Attivo = 1 },
            new() { Nome = "Anna", Cognome = "Blu", Attivo = 1 }
        };
        db.Allievis.AddRange(allievi);
        await db.SaveChangesAsync();

        // 4. Iscrizioni
        db.Iscrizionis.AddRange(
            new Iscrizioni { AllievoId = 1, CorsoId = 1, Attivo = 1, DataIscrizione = DateTime.Now.ToString() },
            new Iscrizioni { AllievoId = 2, CorsoId = 1, Attivo = 1, DataIscrizione = DateTime.Now.ToString() },
            new Iscrizioni { AllievoId = 3, CorsoId = 2, Attivo = 1, DataIscrizione = DateTime.Now.ToString() },
            new Iscrizioni { AllievoId = 4, CorsoId = 3, Attivo = 1, DataIscrizione = DateTime.Now.ToString() }
        );

        // 5. Lezioni ricorrenti per questa settimana
        var lezioni = new List<Lezioni>
        {
            // Lunedì
            new() { CorsoId = 1, InsegnanteId = 1, GiornoSettimana = 1, OraInizio = "10:00", OraFine = "11:00" },
            new() { CorsoId = 2, InsegnanteId = 2, GiornoSettimana = 1, OraInizio = "15:30", OraFine = "17:00" },
            // Martedì  
            new() { CorsoId = 1, InsegnanteId = 1, GiornoSettimana = 2, OraInizio = "11:00", OraFine = "12:30" },
            new() { CorsoId = 3, InsegnanteId = 1, GiornoSettimana = 2, OraInizio = "17:00", OraFine = "18:00" },
            // Mercoledì
            new() { CorsoId = 2, InsegnanteId = 2, GiornoSettimana = 3, OraInizio = "14:00", OraFine = "15:30" },
            // Giovedì
            new() { CorsoId = 1, InsegnanteId = 1, GiornoSettimana = 4, OraInizio = "10:30", OraFine = "12:00" },
            // Venerdì
            new() { CorsoId = 3, InsegnanteId = 1, GiornoSettimana = 5, OraInizio = "16:00", OraFine = "18:00" },
            // Sabato
            new() { CorsoId = 2, InsegnanteId = 2, GiornoSettimana = 6, OraInizio = "10:00", OraFine = "11:30" }
        };
        db.Lezionis.AddRange(lezioni);
        await db.SaveChangesAsync();
    }
}