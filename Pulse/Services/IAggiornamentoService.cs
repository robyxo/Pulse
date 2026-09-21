namespace Pulse.Services;

public interface IAggiornamentoService
{
    /// <summary>Versione installata, come la vede il sistema di aggiornamento.</summary>
    string VersioneCorrente { get; }

    /// <summary>
    /// true solo se l'app gira da un'installazione fatta con il programma di
    /// installazione. Dal progetto in Debug è sempre false.
    /// </summary>
    bool InstallazioneGestita { get; }

    Task<(bool CeUnAggiornamento, string? NuovaVersione, string? Errore)> ControllaAggiornamentiAsync();

    Task<(bool Successo, string? Errore)> ScaricaEInstallaAsync(IProgress<int>? progresso = null);
}