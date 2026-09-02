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

            // Condivisione o apertura del file per la stampa nativa di sistema
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Stampa Ricevuta di Cortesia",
                File = new ShareFile(tempFile)
            });
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Errore Stampa", $"Impossibile generare la ricevuta: {ex.Message}", "OK");
        }
    }

    private string GeneraHtmlRicevuta(Abbonamenti a, string nomeScuola, string indirizzoScuola, string pivaScuola)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8' />
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; margin: 20px; color: #1e293b; }}
        .box {{ border: 2px solid #cbd5e1; border-radius: 8px; padding: 24px; max-width: 600px; margin: auto; }}
        .header {{ text-align: center; border-bottom: 2px solid #e2e8f0; padding-bottom: 12px; margin-bottom: 20px; }}
        .title {{ font-size: 20px; font-weight: bold; color: #1e40af; margin: 0; }}
        .subtitle {{ font-size: 12px; color: #64748b; margin-top: 4px; }}
        .row {{ display: flex; justify-content: space-between; margin-bottom: 10px; font-size: 14px; }}
        .label {{ font-weight: bold; color: #475569; }}
        .total-box {{ background-color: #f8fafc; border: 1px dashed #94a3b8; border-radius: 6px; padding: 12px; margin-top: 20px; }}
        .total-amount {{ font-size: 18px; font-weight: bold; color: #10b981; text-align: right; }}
        .footer {{ font-size: 11px; color: #94a3b8; text-align: center; margin-top: 24px; }}
    </style>
</head>
<body>
    <div class='box'>
        <div class='header'>
            <div class='title'>{nomeScuola}</div>
            <div class='subtitle'>{indirizzoScuola} | P.IVA/C.F.: {pivaScuola}</div>
            <h3 style='margin-top: 15px; margin-bottom: 0;'>RICEVUTA DI CORTESIA</h3>
            <div class='subtitle'>Data Emissione: {DateTime.Now:dd/MM/yyyy HH:mm}</div>
        </div>

        <div class='row'>
            <span class='label'>Allievo:</span>
            <span>{a.Allievo?.NomeCompleto ?? $"{a.Allievo?.Nome} {a.Allievo?.Cognome}"}</span>
        </div>
        <div class='row'>
            <span class='label'>Codice Fiscale:</span>
            <span>{a.Allievo?.CodiceFiscale ?? "-"}</span>
        </div>
        <div class='row'>
            <span class='label'>Corso / Attività:</span>
            <span>{a.Corso?.Nome ?? "-"}</span>
        </div>
        <div class='row'>
            <span class='label'>Tipologia:</span>
            <span>{a.TipoAbbonamento}</span>
        </div>
        <div class='row'>
            <span class='label'>Validità:</span>
            <span>dal {a.DataInizio:dd/MM/yyyy} al {a.DataScadenza:dd/MM/yyyy}</span>
        </div>

        <div class='total-box'>
            <div class='row' style='margin: 0;'>
                <span class='label' style='font-size: 16px;'>IMPORTO SALDATO:</span>
                <span class='total-amount'>€ {a.ImportoPagato:N2}</span>
            </div>
        </div>

        <div class='footer'>
            Documento non fiscale emesso a titolo di quietanza di pagamento per l'attività svolta.
        </div>
    </div>
</body>
</html>";
    }
}