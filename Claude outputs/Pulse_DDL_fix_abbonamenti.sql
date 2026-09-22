-- =====================================================================
-- PULSE — correzione tipi data su Abbonamenti
--
-- Problema: DataInizio, DataScadenza e DataSospensione sono dichiarate TEXT,
-- quindi lo scaffold le rigenera come 'string' e tutto il codice che fa
-- .Date / .Year / .AddDays / confronti smette di compilare.
--
-- SQLite non sa cambiare il tipo di una colonna: la tabella va ricostruita.
-- I dati vengono copiati, nessuna riga viene persa.
--
-- ATTENZIONE: da eseguire su OGNI database Pulse (quello del repo per lo
-- scaffold e quello di esercizio in AppData), sempre dopo un backup.
-- =====================================================================

PRAGMA foreign_keys = off;

BEGIN TRANSACTION;

CREATE TABLE Abbonamenti_nuovo (
    Id                       INTEGER NOT NULL CONSTRAINT PK_Abbonamenti PRIMARY KEY AUTOINCREMENT,
    AllievoId                INTEGER NOT NULL,
    CorsoId                  INTEGER NOT NULL,
    TipoAbbonamento          TEXT NOT NULL DEFAULT 'Mensile',
    DataInizio               DATETIME NOT NULL,
    DataScadenza             DATETIME NOT NULL,
    ImportoTotale            REAL NOT NULL DEFAULT 0.0,
    ImportoPagato            REAL NOT NULL DEFAULT 0.0,
    IsPagato                 INTEGER NOT NULL DEFAULT 0,
    IsSospeso                INTEGER NOT NULL DEFAULT 0,
    DataSospensione          DATETIME NULL,
    GiorniRimanentiCongelati INTEGER NOT NULL DEFAULT 0,
    Attivo                   INTEGER NOT NULL DEFAULT 1,
    DataPagamento            DATETIME,
    MetodoPagamento          TEXT DEFAULT 'Contanti',
    Stornato                 INTEGER DEFAULT 0,
    DataStorno               DATETIME,
    MotivoStorno             TEXT,
    CONSTRAINT FK_Abbonamenti_Allievi_AllievoId FOREIGN KEY (AllievoId) REFERENCES Allievi (Id) ON DELETE CASCADE,
    CONSTRAINT FK_Abbonamenti_Corsi_CorsoId     FOREIGN KEY (CorsoId)   REFERENCES Corsi (Id)   ON DELETE CASCADE
);

INSERT INTO Abbonamenti_nuovo (
    Id, AllievoId, CorsoId, TipoAbbonamento, DataInizio, DataScadenza,
    ImportoTotale, ImportoPagato, IsPagato, IsSospeso, DataSospensione,
    GiorniRimanentiCongelati, Attivo, DataPagamento, MetodoPagamento,
    Stornato, DataStorno, MotivoStorno)
SELECT
    Id, AllievoId, CorsoId, TipoAbbonamento, DataInizio, DataScadenza,
    ImportoTotale, ImportoPagato, IsPagato, IsSospeso, DataSospensione,
    GiorniRimanentiCongelati, Attivo, DataPagamento, MetodoPagamento,
    Stornato, DataStorno, MotivoStorno
FROM Abbonamenti;

DROP TABLE Abbonamenti;

ALTER TABLE Abbonamenti_nuovo RENAME TO Abbonamenti;

CREATE INDEX IX_Abbonamenti_AllievoId     ON Abbonamenti (AllievoId);
CREATE INDEX IX_Abbonamenti_CorsoId       ON Abbonamenti (CorsoId);
CREATE INDEX IX_Abbonamenti_DataPagamento ON Abbonamenti (DataPagamento);

COMMIT;

PRAGMA foreign_keys = on;

-- Controllo finale: deve rispondere "ok" e non elencare violazioni.
PRAGMA foreign_key_check;
PRAGMA integrity_check;
