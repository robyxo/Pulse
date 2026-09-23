using Pulse.Utils;

namespace Pulse.Services;

public class AvvisiDispositivoService : IAvvisiDispositivoService
{
    private readonly IDatabaseService _databaseService;

    // Su Windows può esserci un solo popup aperto alla volta: due registrazioni
    // ravvicinate dal tablet farebbero andare in errore il secondo. Si mettono in fila.
    private readonly SemaphoreSlim _codaAvvisi = new(1, 1);

    public AvvisiDispositivoService(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task SegnalaNuovaRegistrazioneAsync(int allievoId)
    {
        var allievo = await _databaseService.GetAllievoAsync(allievoId);
        if (allievo == null) return;

        await _codaAvvisi.WaitAsync();
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                // Due bottoni e non un semplice OK: la segreteria potrebbe essere a
                // metà di un'altra scheda non ancora salvata, e portarla via di colpo
                // le farebbe perdere il lavoro. Se sceglie "Più tardi", l'allievo
                // resta segnato in lista con il triangolo giallo e il promemoria
                // compare alla prossima apertura di Gestione Allievi.
                bool apri = await Shell.Current.DisplayAlert(
                    "📝 Nuova registrazione",
                    $"Si è appena registrato {allievo.Nome} {allievo.Cognome}.\n\nVuoi aprire la sua scheda per fare l'abbonamento?",
                    "Apri scheda",
                    "Più tardi");

                if (!apri) return;

                await Shell.Current.GoToAsync(
                    AppRoutes.Allievi.GestioneAllievo,
                    new Dictionary<string, object> { { "Allievo", allievo } });
            });
        }
        catch (Exception ex)
        {
            // Un avviso non riuscito non deve mai far cadere l'app: l'allievo è
            // comunque salvato e segnato come da abbonare.
            System.Diagnostics.Debug.WriteLine($"[AvvisiDispositivo] Avviso non mostrato: {ex.Message}");
        }
        finally
        {
            _codaAvvisi.Release();
        }
    }
}
