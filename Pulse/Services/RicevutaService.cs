using Pulse.Models;

namespace Pulse.Services;

public class RicevutaService
{
    public async Task StampaRicevutaCortesiaAsync(Abbonamenti abbonamento, string nomeScuola, string indirizzoScuola, string pivaScuola)
    {
        try
        {
            var html = GeneraHtmlRicevuta(abbonamento, nomeScuola, indirizzoScuola, pivaScuola);
            var tempFile = Path.Combine(FileSystem.CacheDirectory, $"Ricevuta_{abbonamento.Id}_{DateTime.Now:yyyyMMddHHmmss}.html");

            await File.WriteAllTextAsync(tempFile, html);

            // 🖨️ APRE DIRETTAMENTE IL FILE NEL BROWSER/VIEWER PREDEFINITO PER LA STAMPA
            await Launcher.Default.OpenAsync(new OpenFileRequest
            {
                Title = "Visualizza e Stampa Ricevuta",
                File = new ReadOnlyFile(tempFile)
            });
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Errore Stampa", $"Impossibile aprire la ricevuta: {ex.Message}", "OK");
        }
    }

    private string GeneraHtmlRicevuta(Abbonamenti a, string nomeScuola, string indirizzoScuola, string pivaScuola)
    {
        return $@"
    
    Ricevuta di Cortesia

{nomeScuola}

{indirizzoScuola} | P.IVA/C.F.: {pivaScuola}
RICEVUTA DI CORTESIA

Data Emissione: {DateTime.Now:dd/MM/yyyy HH:mm}

Allievo:
{a.Allievo?.NomeCompleto ?? $"{a.Allievo?.Nome} {a.Allievo?.Cognome}"}

Codice Fiscale:
{a.Allievo?.CodiceFiscale ?? "-"}

Corso / Attività:
{a.Corso?.Nome ?? "-"}

Tipologia:
{a.TipoAbbonamento}

Validità:
dal {a.DataInizio:dd/MM/yyyy} al {a.DataScadenza:dd/MM/yyyy}

IMPORTO SALDATO:
€ {a.ImportoPagato:N2}

Documento non fiscale emesso a titolo di quietanza di pagamento per l'attività svolta.

";
    }
}