using Microsoft.EntityFrameworkCore;
using Pulse.Models;
using System.Data.Common;

namespace Pulse.Services;

/// <summary>
/// Aggiornamento schema del database, dichiarativo e idempotente.
///
/// Per aggiungere una colonna o una tabella in una versione futura basta
/// inserire una riga negli elenchi qui sotto: il servizio confronta lo schema
/// atteso con quello reale (sqlite_master e PRAGMA table_info) e applica solo
/// la differenza. Nessuna riga viene mai modificata o cancellata.
///
/// NOTA: i tipi dichiarati delle colonne gia' esistenti non vengono corretti.
/// A runtime non serve (SQLite e' a tipizzazione dinamica ed EF legge in base
/// al Model), conta solo in fase di scaffold sul PC di sviluppo.
/// </summary>
public class MigrazioneDbService : IMigrazioneDbService
{
    private readonly IDbContextFactory<PulseContext> _contextFactory;

    public MigrazioneDbService(IDbContextFactory<PulseContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    // ------------------------------------------------------------------
    // SCHEMA ATTESO
    // L'ordine conta: le tabelle referenziate da una foreign key vanno prima.
    // ------------------------------------------------------------------

    private static readonly (string Nome, string Ddl)[] TabelleAttese =
    {
        ("Sale", """
            CREATE TABLE Sale (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                Nome        TEXT NOT NULL,
                Descrizione TEXT,
                Capienza    INTEGER DEFAULT 0,
                Colore      TEXT DEFAULT '#0EA5E9',
                Attivo      INTEGER DEFAULT 1)
            """),

        ("Stagioni", """
            CREATE TABLE Stagioni (
                Id         INTEGER PRIMARY KEY AUTOINCREMENT,
                Nome       TEXT NOT NULL,
                DataInizio DATETIME NOT NULL,
                DataFine   DATETIME NOT NULL,
                IsCorrente INTEGER DEFAULT 0,
                Note       TEXT,
                Attivo     INTEGER DEFAULT 1)
            """),

        ("PagamentiInsegnanti", """
            CREATE TABLE PagamentiInsegnanti (
                Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                InsegnanteId    INTEGER NOT NULL REFERENCES Insegnanti (Id),
                DataPagamento   DATETIME NOT NULL,
                PeriodoDal      DATETIME,
                PeriodoAl       DATETIME,
                OreTotali       REAL DEFAULT 0,
                Importo         REAL NOT NULL DEFAULT 0,
                MetodoPagamento TEXT DEFAULT 'Contanti',
                Note            TEXT,
                Attivo          INTEGER DEFAULT 1)
            """),

        ("QuerySalvate", """
            CREATE TABLE QuerySalvate (
                Id                 INTEGER PRIMARY KEY AUTOINCREMENT,
                Nome               TEXT NOT NULL,
                Descrizione        TEXT,
                TestoQuery         TEXT NOT NULL,
                Categoria          TEXT DEFAULT 'Generale',
                SoloAmministratore INTEGER DEFAULT 0,
                Ordine             INTEGER DEFAULT 0,
                UltimaEsecuzione   DATETIME,
                Attivo             INTEGER DEFAULT 1)
            """),

        ("Comunicazioni", """
            CREATE TABLE Comunicazioni (
                Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                DataInvio       DATETIME NOT NULL,
                Tipo            TEXT DEFAULT 'Email',
                Oggetto         TEXT,
                Corpo           TEXT,
                AllievoId       INTEGER REFERENCES Allievi (Id),
                Destinatario    TEXT,
                Esito           INTEGER DEFAULT 1,
                MessaggioErrore TEXT,
                Attivo          INTEGER DEFAULT 1)
            """),

        ("DispositiviAutorizzati", """
            CREATE TABLE DispositiviAutorizzati (
                Id            INTEGER PRIMARY KEY AUTOINCREMENT,
                Nome          TEXT NOT NULL,
                Token         TEXT NOT NULL,
                IndirizzoIp   TEXT,
                UltimoAccesso DATETIME,
                Attivo        INTEGER DEFAULT 1)
            """),

        // Risposte alle domande extra del modulo privacy (una riga per risposta).
        // Senza questa, sui PC delle scuole la stampa col modello dispositivo
        // andrebbe in errore: la tabella esisterebbe solo dove la DDL è stata data a mano.
        ("CampiExtraAllievo", """
            CREATE TABLE CampiExtraAllievo (
                Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                AllievoId       INTEGER NOT NULL REFERENCES Allievi (Id),
                Chiave          TEXT NOT NULL,
                Etichetta       TEXT,
                Valore          TEXT,
                DataInserimento DATETIME,
                Origine         TEXT DEFAULT 'Tablet',
                Attivo          INTEGER DEFAULT 1)
            """),
    };

    private static readonly (string Tabella, string Colonna, string Definizione)[] ColonneAttese =
    {
        ("Lezioni",            "SalaId",              "INTEGER REFERENCES Sale (Id)"),

        ("CalendarioChiusure", "Recupero",            "INTEGER DEFAULT 1"),
        ("CalendarioChiusure", "StagioneId",          "INTEGER REFERENCES Stagioni (Id)"),

        ("Abbonamenti",        "DataPagamento",       "DATETIME"),
        ("Abbonamenti",        "MetodoPagamento",     "TEXT DEFAULT 'Contanti'"),
        ("Abbonamenti",        "Stornato",            "INTEGER DEFAULT 0"),
        ("Abbonamenti",        "DataStorno",          "DATETIME"),
        ("Abbonamenti",        "MotivoStorno",        "TEXT"),

        ("Privacy",            "PresaVisione",        "INTEGER DEFAULT 0"),
        ("Privacy",            "Firmato",             "INTEGER DEFAULT 0"),
        ("Privacy",            "DataFirma",           "DATETIME"),
        ("Privacy",            "PercorsoPdf",         "TEXT"),
        ("Privacy",            "ModelloUsato",        "TEXT"),
        ("Privacy",            "Attivo",              "INTEGER DEFAULT 1"),

        ("Allievi",            "DaAbbonare",          "INTEGER DEFAULT 0"),
        ("Allievi",            "DataRegistrazione",   "DATETIME"),
        ("Allievi",            "Origine",             "TEXT DEFAULT 'Segreteria'"),

        ("Impostazioni",       "StampaSilenziosa",    "INTEGER DEFAULT 0"),
        ("Impostazioni",       "ArchiviaRicevutePdf", "INTEGER DEFAULT 0"),
        ("Impostazioni",       "CartellaRicevutePdf", "TEXT"),
        ("Impostazioni",       "DispositivoPrivacy",  "INTEGER DEFAULT 0"),
        ("Impostazioni",       "CartellaModuliPath",  "TEXT"),
        ("Impostazioni",       "ApiTabletAttiva",     "INTEGER DEFAULT 0"),
        ("Impostazioni",       "ApiTabletPorta",      "INTEGER DEFAULT 5000"),
        ("Impostazioni",       "StagioneCorrenteId",  "INTEGER REFERENCES Stagioni (Id)"),
    };

    private static readonly (string Nome, string Ddl)[] IndiciAttesi =
    {
        ("IX_Lezioni_SalaId",                  "CREATE INDEX IX_Lezioni_SalaId ON Lezioni (SalaId)"),
        ("IX_CalendarioChiusure_StagioneId",   "CREATE INDEX IX_CalendarioChiusure_StagioneId ON CalendarioChiusure (StagioneId)"),
        ("IX_Abbonamenti_DataPagamento",       "CREATE INDEX IX_Abbonamenti_DataPagamento ON Abbonamenti (DataPagamento)"),
        ("IX_PagamentiInsegnanti_InsegnanteId","CREATE INDEX IX_PagamentiInsegnanti_InsegnanteId ON PagamentiInsegnanti (InsegnanteId)"),
        ("IX_PagamentiInsegnanti_DataPagamento","CREATE INDEX IX_PagamentiInsegnanti_DataPagamento ON PagamentiInsegnanti (DataPagamento)"),
        ("IX_Comunicazioni_AllievoId",         "CREATE INDEX IX_Comunicazioni_AllievoId ON Comunicazioni (AllievoId)"),
        ("IX_Comunicazioni_DataInvio",         "CREATE INDEX IX_Comunicazioni_DataInvio ON Comunicazioni (DataInvio)"),
        ("IX_Privacy_Id_Allievo",              "CREATE INDEX IX_Privacy_Id_Allievo ON Privacy (Id_Allievo)"),
        ("IX_DispositiviAutorizzati_Token",    "CREATE UNIQUE INDEX IX_DispositiviAutorizzati_Token ON DispositiviAutorizzati (Token)"),
        ("IX_CampiExtraAllievo_AllievoId",     "CREATE INDEX IX_CampiExtraAllievo_AllievoId ON CampiExtraAllievo (AllievoId)"),
        ("IX_CampiExtraAllievo_Chiave",        "CREATE INDEX IX_CampiExtraAllievo_Chiave ON CampiExtraAllievo (Chiave)"),
        ("IX_CampiExtraAllievo_Allievo_Chiave","CREATE UNIQUE INDEX IX_CampiExtraAllievo_Allievo_Chiave ON CampiExtraAllievo (AllievoId, Chiave)"),
    };

    /// <summary>
    /// Valori iniziali da scrivere SOLO la prima volta che la relativa
    /// tabella o colonna viene creata, mai in seguito.
    /// </summary>
    private static readonly (string Tabella, string Colonna, string Sql)[] RiempimentiIniziali =
    {
        ("Sale", "",
            "INSERT INTO Sale (Nome, Descrizione, Attivo) VALUES ('Sala 1', 'Sala principale', 1)"),

        ("CalendarioChiusure", "Recupero",
            "UPDATE CalendarioChiusure SET Recupero = 1 WHERE Recupero IS NULL"),

        ("Privacy", "Attivo",
            "UPDATE Privacy SET Attivo = 1 WHERE Attivo IS NULL"),

        // Lo storico gia' incassato prende come data di pagamento la data di inizio,
        // altrimenti le statistiche partirebbero vuote.
        ("Abbonamenti", "DataPagamento",
            "UPDATE Abbonamenti SET DataPagamento = DataInizio WHERE DataPagamento IS NULL AND IsPagato = 1"),
    };

    // ------------------------------------------------------------------

    public async Task<int> AggiornaSchemaAsync()
    {
        int modifiche = 0;

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var connessione = context.Database.GetDbConnection();

            if (connessione.State != System.Data.ConnectionState.Open)
            {
                await connessione.OpenAsync();
            }

            var tabelleEsistenti = await LeggiNomiAsync(connessione, "table");
            var indiciEsistenti = await LeggiNomiAsync(connessione, "index");

            // Tiene traccia di cosa e' stato creato adesso, per sapere
            // su cosa applicare i valori iniziali.
            var tabelleCreate = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var colonneCreate = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            bool copiaSicurezzaFatta = false;

            // 1) TABELLE MANCANTI
            foreach (var (nome, ddl) in TabelleAttese)
            {
                if (tabelleEsistenti.Contains(nome)) continue;

                copiaSicurezzaFatta = GarantisciCopiaSicurezza(connessione, copiaSicurezzaFatta);

                if (await EseguiAsync(connessione, ddl))
                {
                    tabelleEsistenti.Add(nome);
                    tabelleCreate.Add(nome);
                    modifiche++;
                }
            }

            // 2) COLONNE MANCANTI
            foreach (var gruppo in ColonneAttese.GroupBy(c => c.Tabella, StringComparer.OrdinalIgnoreCase))
            {
                string tabella = gruppo.Key;
                if (!tabelleEsistenti.Contains(tabella)) continue;

                var colonneEsistenti = await LeggiColonneAsync(connessione, tabella);

                foreach (var (_, colonna, definizione) in gruppo)
                {
                    if (colonneEsistenti.Contains(colonna)) continue;

                    copiaSicurezzaFatta = GarantisciCopiaSicurezza(connessione, copiaSicurezzaFatta);

                    if (await EseguiAsync(connessione, $"ALTER TABLE {tabella} ADD COLUMN {colonna} {definizione}"))
                    {
                        colonneCreate.Add($"{tabella}.{colonna}");
                        modifiche++;
                    }
                }
            }

            // 3) INDICI MANCANTI
            foreach (var (nome, ddl) in IndiciAttesi)
            {
                if (indiciEsistenti.Contains(nome)) continue;

                if (await EseguiAsync(connessione, ddl))
                {
                    modifiche++;
                }
            }

            // 4) VALORI INIZIALI, solo su cio' che e' appena nato
            foreach (var (tabella, colonna, sql) in RiempimentiIniziali)
            {
                bool appenaCreato = string.IsNullOrEmpty(colonna)
                    ? tabelleCreate.Contains(tabella)
                    : colonneCreate.Contains($"{tabella}.{colonna}");

                if (appenaCreato)
                {
                    await EseguiAsync(connessione, sql);
                }
            }

            if (modifiche > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[MigrazioneDb] Schema aggiornato: {modifiche} modifiche applicate.");
            }
        }
        catch (Exception ex)
        {
            // L'avvio dell'app non deve mai morire qui: si registra e si prosegue.
            System.Diagnostics.Debug.WriteLine($"[MigrazioneDb] Errore generale: {ex}");
        }

        return modifiche;
    }

    // ------------------------------------------------------------------
    // SUPPORTO
    // ------------------------------------------------------------------

    /// <summary>
    /// Copia il file del database prima della prima modifica di questa sessione.
    /// Se qualcosa va storto, la scuola ha comunque il suo archivio intatto.
    /// </summary>
    private static bool GarantisciCopiaSicurezza(DbConnection connessione, bool giaFatta)
    {
        if (giaFatta) return true;

        try
        {
            string percorso = connessione.DataSource;
            if (string.IsNullOrWhiteSpace(percorso) || !File.Exists(percorso)) return true;

            string cartella = Path.GetDirectoryName(percorso) ?? string.Empty;
            string nome = Path.GetFileNameWithoutExtension(percorso);
            string destinazione = Path.Combine(
                cartella,
                $"{nome}_prima_aggiornamento_{DateTime.Now:yyyyMMdd_HHmmss}.db");

            // Copia sincrona, di proposito: AggiornaSchemaAsync viene chiamato all'avvio
            // con .GetAwaiter().GetResult() sul thread dell'interfaccia. Un Task.Run qui
            // proverebbe a rientrare su quel thread, che sta aspettando: l'app si blocca
            // senza errori prima di mostrare la finestra. Il file e' piccolo, basta cosi'.
            File.Copy(percorso, destinazione, overwrite: false);

            System.Diagnostics.Debug.WriteLine($"[MigrazioneDb] Copia di sicurezza: {destinazione}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MigrazioneDb] Copia di sicurezza non riuscita: {ex.Message}");
        }

        return true;
    }

    private static async Task<HashSet<string>> LeggiNomiAsync(DbConnection connessione, string tipo)
    {
        var risultato = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using var comando = connessione.CreateCommand();
        comando.CommandText = "SELECT name FROM sqlite_master WHERE type = $tipo";

        var parametro = comando.CreateParameter();
        parametro.ParameterName = "$tipo";
        parametro.Value = tipo;
        comando.Parameters.Add(parametro);

        using var lettore = await comando.ExecuteReaderAsync();
        while (await lettore.ReadAsync())
        {
            risultato.Add(lettore.GetString(0));
        }

        return risultato;
    }

    private static async Task<HashSet<string>> LeggiColonneAsync(DbConnection connessione, string tabella)
    {
        var risultato = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using var comando = connessione.CreateCommand();
        // PRAGMA non accetta parametri: il nome arriva dall'elenco costante qui sopra.
        comando.CommandText = $"PRAGMA table_info(\"{tabella}\")";

        using var lettore = await comando.ExecuteReaderAsync();
        while (await lettore.ReadAsync())
        {
            risultato.Add(lettore.GetString(1));
        }

        return risultato;
    }

    private static async Task<bool> EseguiAsync(DbConnection connessione, string sql)
    {
        try
        {
            using var comando = connessione.CreateCommand();
            comando.CommandText = sql;
            await comando.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MigrazioneDb] Istruzione fallita:\n{sql}\n{ex.Message}");
            return false;
        }
    }
}
