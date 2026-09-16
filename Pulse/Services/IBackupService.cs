namespace Pulse.Services;

public interface IBackupService
{
    Task<(bool Successo, string? PercorsoFile, string? Errore)> EseguiBackupAsync();
    Task<(bool Successo, string? Errore)> RipristinaBackupAsync(string percorsoFileBackup);

    Task<(bool Successo, string? Errore)> ResetApplicazioneAsync();
}