# Pulse — lavoro svolto in autonomia (21/09/2026)

File modificati direttamente in `D:\Progetti\Pulse`. **Niente git, niente scaffold, niente DDL**: tutto resta da compilare e pushare a te.

---

## 1. Abbonamenti persi (punto 1) — `ViewModels/GestioneAllievoViewModel.cs`

Tre punti di perdita, tutti risolti:

- **`CompletaCreazioneAbbonamentoAsync`** — prima, se l'allievo era nuovo (`Id == 0`), l'abbonamento non veniva né salvato né inserito in lista: spariva. Ora l'inserimento in tabella avviene **sempre**; il salvataggio su DB solo se l'allievo esiste già. Il messaggio avvisa che verrà salvato insieme all'anagrafica.
- **`SalvaAllievoAsync`** — il filtro era su `AllievoId == 0`, ma `StampaRicevutaAsync` aveva già valorizzato `AllievoId` senza salvare la riga. Ora il filtro è su `a.Id == 0` (record non ancora a database).
- **`StampaRicevutaAsync`** — se stampi la ricevuta di un abbonamento non ancora salvato e l'allievo nel frattempo è stato creato, l'abbonamento viene salvato prima della stampa.
- Ripulito anche il warning **CS8601** in `RinnovaAbbonamentoAsync` (`abbonamento.Corso` nullable).

## 2. Solo l'ultimo abbonamento per corso + storico (punto 2)

- `ApplicaFiltroEPaginazione` ora fa `GroupBy(CorsoId)` e tiene **l'abbonamento più recente per ogni corso**. A parità di data vince l'ultimo inserito (anche se non ancora salvato).
- Nuovo comando `ApriStoricoAbbonamentiAsync` → apre lo storico completo di **quel corso per quell'allievo**.
- **Nuovo popup** `Views/Popups/StoricoAbbonamentiPage.xaml` + `.xaml.cs`: elenco periodi, tipo, importo, stato colorato (usa `StatoAbbonamentoHelper`) e **totale incassato** in fondo. Costruito con `new` come `NuovoAbbonamentoPage` → **nessuna registrazione DI necessaria**.
- `Views/GestioneAllievoPage.xaml`: aggiunto il bottone **📋** (viola `#EDE9FE`) come primo della riga azioni, prima di 🖨️.

### Bonus: filtro anni che non si aggiornava
Estratto `AggiornaAnniDisponibili()`, richiamato dopo **nuovo abbonamento, rinnovo ed eliminazione**. Prima un abbonamento di un anno nuovo restava invisibile nel filtro. La selezione corrente viene mantenuta se l'anno esiste ancora.

## 3. Ricevuta 90×90 in alto a destra su A4 (punto 4) — `Services/RicevutaService.cs`

Il foglio viene ora trattato come **A4 verticale con margine 0**, e il foglietto è posizionato in **assoluto nell'angolo in alto a destra**, dove la scuola appoggia i foglietti nel vassoio.

Rimappati i campi già esistenti in Impostazioni (nessuna modifica al DB):

| Campo | Significato ora | Default |
|---|---|---|
| `RicevutaLarghezzaMm` | larghezza foglietto | 90 |
| `RicevutaAltezzaMm` | altezza foglietto | 90 |
| `RicevutaMarginTopMm` | **distanza dal bordo alto del foglio** | 10 |
| `RicevutaMarginRightMm` | **distanza dal bordo destro del foglio** | 10 (era 3) |
| `RicevutaMarginLeftMm` | margine interno del foglietto | 5 |
| `RicevutaMarginBottomMm` | **non più usato** | — |

Ingranditi i corpi testo (body 12px, intestazione 14px, pagamento 13px) perché ora c'è più spazio utile. Il bottone «Stampa» è a fondo pagina a sinistra e resta nascosto in stampa.

> **Da fare in Impostazioni quando vuoi**: aggiornare le etichette dei tre campi ("distanza dall'alto", "distanza da destra", "margine interno") e nascondere quello inferiore. Non l'ho toccato per non stravolgere la pagina.

## 4. Orario scaglionato (punto 6) — `Views/CalendarioPage.xaml.cs`

`GeneraGrigliaAOrario` filtrava con `inizio >= slot && inizio < fineSlot`: una lezione 09:00–10:00 con intervallo 30 min restava solo nella riga delle 09:00.

Ora il test è di **sovrapposizione** (`inizio < fineSlot && fine > inizioSlot`), quindi la lezione occupa **tutte** le righe che attraversa. Le righe successive alla prima mostrano il badge come **proseguimento**: opacità 0.55 e testo `↳ fino alle 10:00` invece dell'orario completo, così non sembrano due lezioni distinte. La griglia a bande è rimasta identica.

---

## Cosa devi fare tu

1. **Compilare** e provare: nuovo abbonamento su allievo nuovo, storico 📋, stampa ricevuta, calendario a orario con intervallo 30 min.
2. **Push su GitHub** quando ti torna.
3. `git rm "Pulse/DataBase/Pulse - Copia.db"` — è ancora tracciato.

**Nessuna registrazione DI o rotta da aggiungere**: il popup dello storico si istanzia con `new`, esattamente come `NuovoAbbonamentoPage`.

## Cosa NON ho toccato (in attesa di te)

- **Punto 3 — stampa silenziosa via WebView2**: volevo farla insieme, è la più rischiosa e non testabile senza stampante.
- **Tutto ciò che richiede DDL**: Sale + `Lezioni.SalaId`, chiusure con flag recupero, Stagioni, prerequisiti statistiche (`DataPagamento`, pagamenti maestri), Comunicazioni, privacy (template + flag "firmato" + cartella PDF per anno), tablet (allievi "da abbonare" + dispositivi autorizzati).
- Popup unificati (`ShowActionSheet`/`ShowPrompt`), pulizia warning `x:DataType`, pagina Statistiche e bottone **+** query salvate, QR kiosk tablet, release GitHub.
