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
    Task<(bool Successo, int AbbonamentiEstesi)> SalvaChiusuraAsync(CalendarioChiusure chiusura);
    Task<bool> EliminaChiusuraAsync(int id);

    // ================================================
    // PRIVACY
    // ================================================
    Task<List<Privacy>> GetPrivacyAllievoAsync(int allievoId);
    Task<bool> SalvaPrivacyAsync(Privacy privacy);
}