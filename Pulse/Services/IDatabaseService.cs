using Pulse.Models;

namespace Pulse.Services;

public interface IDatabaseService
{
    // ================================================
    // GESTIONE CORSI
    // ================================================
    Task<List<Corsi>> GetCorsiAttiviAsync();
    Task<List<Corsi>> GetCorsiAttivi(); // Alias per compatibilità
    Task<bool> SalvaCorsoAsync(Corsi corso);
    Task<bool> EliminaCorsoAsync(int id);
    Task<bool> EliminaCorsoAsync(Corsi corso); // Alias con oggetto

    // ================================================
    // CALENDARIO E LEZIONI
    // ================================================
    Task<List<Lezioni>> GetLezioniSettimanaAsync(DateTime dataRiferimento);
    Task<List<Lezioni>> GetLezioniSettimana(DateTime dataRiferimento); // Alias
    Task<bool> SalvaLezioneAsync(Lezioni lezione);
    Task<bool> SalvaLezione(Lezioni lezione); // Alias

    // ================================================
    // MAESTRI
    // ================================================
    Task<List<Insegnanti>> GetInsegnantiAttiviAsync();
    Task<bool> SalvaInsegnanteAsync(Insegnanti insegnante);
    Task<bool> EliminaInsegnanteAsync(int id);
    Task<List<Insegnanti>> GetInsegnantiAttivi(); // Alias

    // ================================================
    // ALLIEVI
    // ================================================

    Task<List<Allievi>> GetAllieviAttiviAsync();
    Task<List<Allievi>> GetAllieviPerCorsoAsync(int corsoId);
    Task<List<Allievi>> GetAllieviPerCorso(int corsoId); // Alias
    Task<List<Allievi>> GetAllieviPerLezioneAsync(int lezioneId);
    Task<bool> SalvaAllievoAsync(Allievi allievo);
    Task<bool> EliminaAllievoAsync(int id);
}