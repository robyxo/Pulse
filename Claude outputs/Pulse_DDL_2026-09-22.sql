-- =====================================================================
-- PULSE — DDL per le nuove funzionalita'
-- Data: 22/09/2026
-- Convenzioni rispettate: Id INTEGER PRIMARY KEY AUTOINCREMENT,
-- Attivo INTEGER DEFAULT 1 (soft-delete), flag INTEGER 0/1,
-- importi REAL, date DATETIME (come Abbonamenti), indici IX_Tabella_Colonna.
--
-- ATTENZIONE: fai un backup del DB prima di eseguire.
-- I blocchi sono indipendenti: puoi applicarli tutti o solo quelli che vuoi.
-- =====================================================================


-- =====================================================================
-- BLOCCO A — SALE E CONFLITTI ORARI
-- =====================================================================

CREATE TABLE Sale (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    Nome        TEXT NOT NULL,
    Descrizione TEXT,
    Capienza    INTEGER DEFAULT 0,
    Colore      TEXT DEFAULT '#0EA5E9',
    Attivo      INTEGER DEFAULT 1
);

ALTER TABLE Lezioni ADD COLUMN SalaId INTEGER REFERENCES Sale (Id);

CREATE INDEX IX_Lezioni_SalaId ON Lezioni (SalaId);

-- Sala di default, cosi' le lezioni esistenti non restano orfane.
INSERT INTO Sale (Nome, Descrizione, Attivo) VALUES ('Sala 1', 'Sala principale', 1);


-- =====================================================================
-- BLOCCO B — STAGIONI E CHIUSURE CON RECUPERO
-- =====================================================================

CREATE TABLE Stagioni (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    Nome        TEXT NOT NULL,
    DataInizio  DATETIME NOT NULL,
    DataFine    DATETIME NOT NULL,
    IsCorrente  INTEGER DEFAULT 0,
    Note        TEXT,
    Attivo      INTEGER DEFAULT 1
);

-- Flag "recupero": se 1 la chiusura sposta in avanti la scadenza degli abbonamenti.
-- Default 1 = spunta gia' attiva, sia sulle esistenti sia sulle nuove.
ALTER TABLE CalendarioChiusure ADD COLUMN Recupero INTEGER DEFAULT 1;
UPDATE CalendarioChiusure SET Recupero = 1 WHERE Recupero IS NULL;

-- Collega una chiusura alla stagione (serve per la chiusura stagionale).
ALTER TABLE CalendarioChiusure ADD COLUMN StagioneId INTEGER REFERENCES Stagioni (Id);
CREATE INDEX IX_CalendarioChiusure_StagioneId ON CalendarioChiusure (StagioneId);


-- =====================================================================
-- BLOCCO C — PREREQUISITI STATISTICHE (entrate e uscite)
-- =====================================================================

-- Quando e' entrato davvero il denaro (DataInizio e' la validita', non l'incasso).
ALTER TABLE Abbonamenti ADD COLUMN DataPagamento    DATETIME;
ALTER TABLE Abbonamenti ADD COLUMN MetodoPagamento  TEXT DEFAULT 'Contanti';

-- Storno: un abbonamento annullato/rimborsato non deve sparire dallo storico,
-- deve solo smettere di contare tra le entrate.
ALTER TABLE Abbonamenti ADD COLUMN Stornato     INTEGER DEFAULT 0;
ALTER TABLE Abbonamenti ADD COLUMN DataStorno   DATETIME;
ALTER TABLE Abbonamenti ADD COLUMN MotivoStorno TEXT;

-- Allinea lo storico esistente: gli abbonamenti gia' pagati prendono
-- come data di incasso la loro data di inizio.
UPDATE Abbonamenti SET DataPagamento = DataInizio WHERE DataPagamento IS NULL AND IsPagato = 1;

CREATE INDEX IX_Abbonamenti_DataPagamento ON Abbonamenti (DataPagamento);

-- Uscite: quanto paghi ai maestri.
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
    Attivo          INTEGER DEFAULT 1
);

CREATE INDEX IX_PagamentiInsegnanti_InsegnanteId  ON PagamentiInsegnanti (InsegnanteId);
CREATE INDEX IX_PagamentiInsegnanti_DataPagamento ON PagamentiInsegnanti (DataPagamento);


-- =====================================================================
-- BLOCCO D — QUERY SALVATE (bottone + nella pagina Statistiche)
-- =====================================================================

CREATE TABLE QuerySalvate (
    Id                 INTEGER PRIMARY KEY AUTOINCREMENT,
    Nome               TEXT NOT NULL,
    Descrizione        TEXT,
    TestoQuery         TEXT NOT NULL,
    Categoria          TEXT DEFAULT 'Generale',
    SoloAmministratore INTEGER DEFAULT 0,
    Ordine             INTEGER DEFAULT 0,
    UltimaEsecuzione   DATETIME,
    Attivo             INTEGER DEFAULT 1
);


-- =====================================================================
-- BLOCCO E — COMUNICAZIONI INVIATE (storico notifiche)
-- =====================================================================

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
    Attivo          INTEGER DEFAULT 1
);

CREATE INDEX IX_Comunicazioni_AllievoId ON Comunicazioni (AllievoId);
CREATE INDEX IX_Comunicazioni_DataInvio ON Comunicazioni (DataInvio);


-- =====================================================================
-- BLOCCO F — PRIVACY: PRESA VISIONE, FIRMA E ARCHIVIO PDF
-- =====================================================================

ALTER TABLE Privacy ADD COLUMN PresaVisione INTEGER DEFAULT 0;
ALTER TABLE Privacy ADD COLUMN Firmato      INTEGER DEFAULT 0;
ALTER TABLE Privacy ADD COLUMN DataFirma    DATETIME;
ALTER TABLE Privacy ADD COLUMN PercorsoPdf  TEXT;
ALTER TABLE Privacy ADD COLUMN ModelloUsato TEXT;
ALTER TABLE Privacy ADD COLUMN Attivo       INTEGER DEFAULT 1;

UPDATE Privacy SET Attivo = 1 WHERE Attivo IS NULL;

CREATE INDEX IX_Privacy_Id_Allievo ON Privacy (Id_Allievo);


-- =====================================================================
-- BLOCCO G — TABLET IN SALA (dispositivi autorizzati + allievi da abbonare)
-- =====================================================================

CREATE TABLE DispositiviAutorizzati (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    Nome          TEXT NOT NULL,
    Token         TEXT NOT NULL,
    IndirizzoIp   TEXT,
    UltimoAccesso DATETIME,
    Attivo        INTEGER DEFAULT 1
);

CREATE UNIQUE INDEX IX_DispositiviAutorizzati_Token ON DispositiviAutorizzati (Token);

-- Un allievo registrato dal tablet arriva senza abbonamento: la segreteria lo completa.
ALTER TABLE Allievi ADD COLUMN DaAbbonare        INTEGER DEFAULT 0;
ALTER TABLE Allievi ADD COLUMN DataRegistrazione DATETIME;
ALTER TABLE Allievi ADD COLUMN Origine           TEXT DEFAULT 'Segreteria';


-- =====================================================================
-- BLOCCO H — NUOVE IMPOSTAZIONI
-- =====================================================================

-- Stampa
ALTER TABLE Impostazioni ADD COLUMN StampaSilenziosa      INTEGER DEFAULT 0;
ALTER TABLE Impostazioni ADD COLUMN ArchiviaRicevutePdf   INTEGER DEFAULT 0;
ALTER TABLE Impostazioni ADD COLUMN CartellaRicevutePdf   TEXT;

-- Privacy
ALTER TABLE Impostazioni ADD COLUMN DispositivoPrivacy    INTEGER DEFAULT 0;
ALTER TABLE Impostazioni ADD COLUMN CartellaModuliPath    TEXT;

-- Tablet / API
ALTER TABLE Impostazioni ADD COLUMN ApiTabletAttiva       INTEGER DEFAULT 0;
ALTER TABLE Impostazioni ADD COLUMN ApiTabletPorta        INTEGER DEFAULT 5000;

-- Stagione corrente (comodo averla a portata di mano nelle impostazioni)
ALTER TABLE Impostazioni ADD COLUMN StagioneCorrenteId    INTEGER REFERENCES Stagioni (Id);
