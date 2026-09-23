-- =====================================================================
-- PULSE — Campi extra del modulo privacy/iscrizione
--
-- Ogni scuola ha un modulo diverso: i campi "in più" (professione, come ci
-- hai conosciuto, taglia maglietta, ...) non possono essere colonne fisse.
-- Qui ogni valore è una riga, identificata da una Chiave che arriva dal
-- modello HTML della scuola.
--
-- Le statistiche si fanno raggruppando per Chiave:
--   SELECT Valore, COUNT(*) FROM CampiExtraAllievo
--   WHERE Chiave = 'CONOSCENZA' AND Attivo = 1 GROUP BY Valore;
--
-- ATTENZIONE: fai un backup del DB prima di eseguire.
-- =====================================================================

CREATE TABLE CampiExtraAllievo (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    AllievoId       INTEGER NOT NULL REFERENCES Allievi (Id),

    -- Codice del campo, come scritto nel modello HTML: PROFESSIONE, CONOSCENZA, ...
    Chiave          TEXT NOT NULL,

    -- Etichetta leggibile, copiata qui apposta: se un domani il modello cambia,
    -- le risposte già raccolte restano comprensibili nelle statistiche.
    Etichetta       TEXT,

    Valore          TEXT,

    DataInserimento DATETIME,

    -- Da dove arriva la risposta: 'Tablet' quando la compila l'allievo,
    -- 'Segreteria' se un domani la si vorrà inserire anche dal PC.
    Origine         TEXT DEFAULT 'Tablet',

    Attivo          INTEGER DEFAULT 1
);

CREATE INDEX IX_CampiExtraAllievo_AllievoId ON CampiExtraAllievo (AllievoId);
CREATE INDEX IX_CampiExtraAllievo_Chiave    ON CampiExtraAllievo (Chiave);

-- Un solo valore per campo per allievo: se risponde due volte, si aggiorna.
CREATE UNIQUE INDEX IX_CampiExtraAllievo_Allievo_Chiave
    ON CampiExtraAllievo (AllievoId, Chiave);
