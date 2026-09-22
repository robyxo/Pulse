namespace Pulse.Services;

/// <summary>
/// Allinea lo schema del database a quello che il codice si aspetta.
///
/// Serve perche' EnsureCreated() crea il database solo se non esiste: su
/// un'installazione gia' avviata non aggiunge le colonne e le tabelle nuove.
/// Senza questo passaggio, una scuola che aggiorna Pulse si ritroverebbe
/// l'app in errore su ogni query che tocca i campi aggiunti dopo.
/// </summary>
public interface IMigrazioneDbService
{
    /// <summary>
    /// Aggiunge SOLO cio' che manca (tabelle, colonne, indici) e non tocca
    /// mai i dati esistenti. E' idempotente: eseguirlo a ogni avvio non
    /// cambia nulla quando il database e' gia' aggiornato.
    /// </summary>
    /// <returns>Numero di modifiche applicate; 0 se non c'era niente da fare.</returns>
    Task<int> AggiornaSchemaAsync();
}
