using CommunityToolkit.Maui.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Pulse.Models;

namespace Pulse.Services;

public class BackupService : IBackupService
{
    private readonly IDbContextFactory<PulseContext> _contextFactory;
    private readonly IImpostazioniService _impostazioniService;

    public BackupService(IDbContextFactory<PulseContext> contextFactory, IImpostazioniService impostazioniService)
    {
        _contextFactory = contextFactory;
        _impostazioniService = impostazioniService;
    }

    private static string PercorsoDatabaseCorrente =>
        Path.Combine(FileSystem.AppDataDirectory, "Pulse.db");

    public async Task<(bool Successo, string? PercorsoFile, string? Errore)> EseguiBackupAsync()
    {
        try
        {
            var risultatoCartella = await FolderPicker.Default.PickAsync(CancellationToken.None);

            if (!risultatoCartella.IsSuccessful || risultatoCartella.Folder == null)
            {
                return (false, null, null); // utente ha annullato: nessun errore da mostrare
            }

            string cartellaDestinazione = risultatoCartella.Folder.Path;
            string nomeFile = $"Pulse_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
            string percorsoDestinazione = Path.Combine(cartellaDestinazione, nomeFile);

            // Aggiorniamo e salviamo i metadati PRIMA di copiare, così il backup li contiene già
            var impostazioni = await _impostazioniService.GetImpostazioniAsync();
            impostazioni.BackupPath = cartellaDestinazione;
            impostazioni.UltimoBackupData = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            await _impostazioniService.SalvaImpostazioniAsync(impostazioni);

            // Chiude la connessione per rilasciare il file prima di copiarlo
            SqliteConnection.ClearAllPools();

            File.Copy(PercorsoDatabaseCorrente, percorsoDestinazione, overwrite: true);

            return (true, percorsoDestinazione, null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public async Task<(bool Successo, string? Errore)> RipristinaBackupAsync(string percorsoFileBackup)
    {
        try
        {
            if (!File.Exists(percorsoFileBackup))
            {
                return (false, "Il file selezionato non esiste più.");
            }

            // Chiude la connessione corrente per rilasciare Pulse.db prima di sovrascriverlo
            SqliteConnection.ClearAllPools();

            File.Copy(percorsoFileBackup, PercorsoDatabaseCorrente, overwrite: true);

            // Scrive la data di ripristino direttamente nel nuovo file appena copiato,
            // con una connessione "grezza" separata dal DbContext (che resta legato
            // alla vecchia struttura in memoria finché l'app non viene riavviata).
            try
            {
                await using var connessione = new SqliteConnection($"Data Source={PercorsoDatabaseCorrente}");
                await connessione.OpenAsync();

                await using var comando = connessione.CreateCommand();
                comando.CommandText = "UPDATE Impostazioni SET UltimoRipristinoData = $data";
                comando.Parameters.AddWithValue("$data", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                await comando.ExecuteNonQueryAsync();
            }
            catch
            {
                // Non blocchiamo il ripristino se questo dettaglio non va a buon fine
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
    public async Task<(bool Successo, string? Errore)> ResetApplicazioneAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Una transazione sola: se qualcosa va storto a metà, non resta
            // un database svuotato per metà.
            await using var transazione = await context.Database.BeginTransactionAsync();

            // ExecuteDeleteAsync cancella con una singola istruzione SQL per tabella,
            // senza caricare le righe in memoria.
            // L'ordine rispetta le foreign key: prima le tabelle figlie, poi le padre.
            await context.Presenzes.ExecuteDeleteAsync();
            await context.Abbonamentis.ExecuteDeleteAsync();
            await context.Iscrizionis.ExecuteDeleteAsync();
            await context.Privacies.ExecuteDeleteAsync();
            await context.Lezionis.ExecuteDeleteAsync();
            await context.CalendarioChiusures.ExecuteDeleteAsync();
            await context.Corsis.ExecuteDeleteAsync();
            await context.Allievis.ExecuteDeleteAsync();
            await context.Insegnantis.ExecuteDeleteAsync();
            await context.Impostazionis.ExecuteDeleteAsync();

            await transazione.CommitAsync();

            // Ripulisce anche il file del logo eventualmente caricato
            string cartellaLoghi = Path.Combine(FileSystem.AppDataDirectory, "Logo");
            if (Directory.Exists(cartellaLoghi))
            {
                Directory.Delete(cartellaLoghi, recursive: true);
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}