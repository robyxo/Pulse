using Pulse.Models;

namespace Pulse.Services;

public interface IDatabaseService
{
    // ================================================
    // GESTIONE CORSI
    // ================================================
    Task<List<Corsi>> GetCorsiAttiviAsync();
    Task<bool> SalvaCorsoAsync(Corsi corso);
    Task<bool> EliminaCorsoAsync(int id);

    // ================================================
    // CALENDARIO E LEZIONI
    // ================================================
    Task<List<Lezioni>> GetLezioniRicorrentiAsync();
    Task<bool> SalvaLezioneAsync(Lezioni lezione);
    Task<bool> EliminaLezioneAsync(int id);
    Task<List<Lezioni>> GetLezioniPerCorsoAsync(int corsoId);

    // ================================================
    // SALE
    // ================================================
    Task<List<Sale>> GetSaleAttiveAsync();
    Task<bool> SalvaSalaAsync(Sale sala);
    Task<bool> EliminaSalaAsync(int id);

    /// <summary>
    /// Lezioni gia' presenti in una sala in un dato giorno, esclusa quella che
    /// si sta modificando. Serve a segnalare le sovrapposizioni di orario.
    /// </summary>
    Task<List<Lezioni>> GetLezioniPerSalaEGiornoAsync(int salaId, int giornoSettimana, int lezioneDaEscludereId);

    // ================================================
    // MAESTRI
    // ================================================
    Task<List<Insegnanti>> GetInsegnantiAttiviAsync();
    Task<bool> SalvaInsegnanteAsync(Insegnanti insegnante);
    Task<bool> EliminaInsegnanteAsync(int id);
    Task<List<Lezioni>> GetLezioniPerInsegnanteAsync(int insegnanteId);

    // ================================================
    // ALLIEVI
    // ================================================
    Task<Allievi?> GetAllievoAsync(int id);
    Task<List<Allievi>> GetAllieviAttiviAsync();
    Task<List<Allievi>> GetAllieviPerCorsoAsync(int corsoId);
    Task<bool> SalvaAllievoAsync(Allievi allievo);
    Task<bool> EliminaAllievoAsync(int id);

    // ================================================
    // ABBONAMENTI
    // ================================================
    Task<List<Abbonamenti>> GetAbbonamentiAllievoAsync(int allievoId);
    Task<bool> SalvaAbbonamentoAsync(Abbonamenti abbonamento);
    Task<bool> EliminaAbbonamentoAsync(int id);
    Task<List<Abbonamenti>> GetAbbonamentiAttiviAsync();
    Task<List<Abbonamenti>> GetAbbonamentiAttiviPerCorsoAsync(int corsoId);

    // ================================================
    // CALENDARIO CHIUSURE
    // ================================================
    Task<List<CalendarioChiusure>> GetChiusureAsync();
    /// <summary>
    /// Salva una chiusura o un evento. Alla sola creazione, e solo per le chiusure,
    /// agisce sugli abbonamenti sovrapposti: li prolunga se il recupero è attivo,
    /// oppure li fa scadere il giorno di chiusura se è una chiusura stagionale.
    /// </summary>
    Task<(bool Successo, int AbbonamentiEstesi, int AbbonamentiChiusi)> SalvaChiusuraAsync(CalendarioChiusure chiusura);
    Task<bool> EliminaChiusuraAsync(int id);

    // ================================================
    // COMUNICAZIONI INVIATE
    // ================================================

    /// <summary>Storico degli invii, dal più recente.</summary>
    Task<List<Comunicazioni>> GetComunicazioniAsync(int limite = 200);

    /// <summary>
    /// Registra un invio: una riga per invio, con l'elenco dei destinatari
    /// separati da ';'. Vengono registrati anche i tentativi falliti.
    /// </summary>
    Task<bool> RegistraComunicazioneAsync(
        string tipo,
        string? oggetto,
        string? corpo,
        IEnumerable<string> destinatari,
        bool esito,
        string? messaggioErrore = null,
        int? allievoId = null);

    // ================================================
    // CAMPI EXTRA DEL MODULO (professione, come ci hai conosciuto, ...)
    // ================================================
    // Le chiavi arrivano dal modello HTML della scuola, quindi cambiano da
    // scuola a scuola: qui ogni risposta è una riga, non una colonna.

    Task<List<CampiExtraAllievo>> GetCampiExtraAllievoAsync(int allievoId);

    /// <summary>
    /// Salva o aggiorna la risposta di un allievo a un campo. Un solo valore
    /// per campo: rispondere di nuovo sovrascrive il precedente.
    /// </summary>
    Task<bool> SalvaCampoExtraAsync(int allievoId, string chiave, string? etichetta, string? valore, string origine = "Tablet");

    /// <summary>Conteggio delle risposte per un campo, per le statistiche.</summary>
    Task<List<(string Valore, int Conteggio)>> GetStatisticheCampoExtraAsync(string chiave);

    // ================================================
    // REGISTRAZIONE DA DISPOSITIVO (TABLET)
    // ================================================

    /// <summary>
    /// True se un allievo con questo codice fiscale è già in archivio, anche
    /// se eliminato: il tablet non deve creare doppioni.
    /// </summary>
    Task<bool> EsisteCodiceFiscaleAsync(string codiceFiscale);

    /// <summary>Chiave del QR del tablet. La crea la prima volta.</summary>
    Task<string> GetOCreaChiaveDispositivoAsync();

    /// <summary>
    /// Invalida la chiave attuale e ne crea una nuova: il vecchio QR smette di
    /// funzionare (es. se una foto del QR è finita in giro).
    /// </summary>
    Task<string> RigeneraChiaveDispositivoAsync();

    /// <summary>
    /// Controlla la chiave arrivata dal tablet e, se valida, annota IP e ora
    /// dell'ultimo accesso.
    /// </summary>
    Task<bool> VerificaChiaveDispositivoAsync(string? chiave, string? indirizzoIp);

    // ================================================
    // PRIVACY
    // ================================================
    Task<List<Privacy>> GetPrivacyAllievoAsync(int allievoId);
    Task<bool> SalvaPrivacyAsync(Privacy privacy);
}