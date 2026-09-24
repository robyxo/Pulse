#if WINDOWS
using Velopack;
using Velopack.Sources;
#endif

namespace Pulse.Services;

public class AggiornamentoService : IAggiornamentoService
{
#if WINDOWS

    // Repository PUBBLICO che contiene solo gli installer e gli aggiornamenti
    // (li carica pubblica.cmd). Il codice resta nel repo privato robyxo/Pulse:
    // cosi' Pulse installato nelle scuole scarica gli aggiornamenti senza password.
    private const string UrlRepository = "https://github.com/robyxo/Pulse-Releases";

    private readonly UpdateManager _updateManager;
    private UpdateInfo? _aggiornamentoTrovato;

    public AggiornamentoService()
    {
        _updateManager = new UpdateManager(new GithubSource(UrlRepository, null, false));
    }

    public string VersioneCorrente =>
        _updateManager.CurrentVersion?.ToString() ?? AppInfo.Current.VersionString;

    public bool InstallazioneGestita => _updateManager.IsInstalled;

    public async Task<(bool CeUnAggiornamento, string? NuovaVersione, string? Errore)> ControllaAggiornamentiAsync()
    {
        if (!_updateManager.IsInstalled)
            return (false, null, "Pulse non è stato installato con il programma di installazione, quindi non può aggiornarsi da solo.");

        try
        {
            _aggiornamentoTrovato = await _updateManager.CheckForUpdatesAsync();

            if (_aggiornamentoTrovato == null)
                return (false, null, null);   // già aggiornato

            return (true, _aggiornamentoTrovato.TargetFullRelease.Version.ToString(), null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public async Task<(bool Successo, string? Errore)> ScaricaEInstallaAsync(IProgress<int>? progresso = null)
    {
        if (_aggiornamentoTrovato == null)
            return (false, "Nessun aggiornamento da installare: esegui prima il controllo.");

        try
        {
            await _updateManager.DownloadUpdatesAsync(_aggiornamentoTrovato, p => progresso?.Report(p));

            // Da qui in poi l'app viene chiusa e riaperta aggiornata:
            // la riga successiva non viene mai raggiunta.
            _updateManager.ApplyUpdatesAndRestart(_aggiornamentoTrovato);
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

#else

    // Su piattaforme diverse da Windows il pacchetto Velopack non è referenziato:
    // il servizio esiste comunque, ma non fa niente.
    public string VersioneCorrente => AppInfo.Current.VersionString;

    public bool InstallazioneGestita => false;

    public Task<(bool CeUnAggiornamento, string? NuovaVersione, string? Errore)> ControllaAggiornamentiAsync() =>
        Task.FromResult<(bool, string?, string?)>((false, null, "Gli aggiornamenti automatici sono disponibili solo su Windows."));

    public Task<(bool Successo, string? Errore)> ScaricaEInstallaAsync(IProgress<int>? progresso = null) =>
        Task.FromResult<(bool, string?)>((false, "Gli aggiornamenti automatici sono disponibili solo su Windows."));

#endif
}