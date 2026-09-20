using Pulse.Helpers;
using Pulse.Models;

namespace Pulse.Services;

public class RicevutaService
{
    private readonly IImpostazioniService _impostazioniService;
    private readonly IDatabaseService _databaseService;

    public RicevutaService(IImpostazioniService impostazioniService, IDatabaseService databaseService)
    {
        _impostazioniService = impostazioniService;
        _databaseService = databaseService;
    }

    public async Task StampaRicevutaCortesiaAsync(Abbonamenti abbonamento)
    {
        try
        {
            var impostazioni = await _impostazioniService.GetImpostazioniAsync();

            string nomeScuola = string.IsNullOrWhiteSpace(impostazioni.NomeScuola)
                ? "ASD SCUOLA DI DANZA PULSE"
                : impostazioni.NomeScuola;

            string indirizzoScuola = impostazioni.IndirizzoScuola ?? string.Empty;
            string pivaScuola = impostazioni.PartitaIva ?? string.Empty;

            string logoBase64 = string.Empty;
            if (!string.IsNullOrWhiteSpace(impostazioni.LogoPath) && File.Exists(impostazioni.LogoPath))
            {
                byte[] bytes = await File.ReadAllBytesAsync(impostazioni.LogoPath);
                string estensione = Path.GetExtension(impostazioni.LogoPath).TrimStart('.').ToLowerInvariant();
                string mime = estensione switch
                {
                    "png" => "image/png",
                    "jpg" or "jpeg" => "image/jpeg",
                    "gif" => "image/gif",
                    "bmp" => "image/bmp",
                    _ => "image/png"
                };
                logoBase64 = $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
            }

            var lezioni = await _databaseService.GetLezioniPerCorsoAsync(abbonamento.CorsoId);
            string orario = CalcolaOrario(lezioni);

            var html = GeneraHtmlRicevuta(abbonamento, impostazioni, nomeScuola, indirizzoScuola, pivaScuola, logoBase64, orario);
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

    private string CalcolaOrario(List<Lezioni> lezioni)
    {
        if (lezioni == null || lezioni.Count == 0) return string.Empty;

        var pezzi = lezioni
            .OrderBy(l => l.GiornoSettimana)
            .ThenBy(l => l.OraInizio)
            .Select(l =>
            {
                string giorno = DateHelper.GetNomeGiornoDaDb(l.GiornoSettimana);
                return $"{giorno} {l.OraInizio}-{l.OraFine}".Trim();
            });

        return string.Join(", ", pezzi);
    }

    private string GeneraHtmlRicevuta(Abbonamenti a, Impostazioni impostazioni, string nomeScuola, string indirizzoScuola, string pivaScuola, string logoBase64, string orario)
    {
        int larghezza = impostazioni.RicevutaLarghezzaMm ?? 90;
        int altezza = impostazioni.RicevutaAltezzaMm ?? 90;
        int marginTop = impostazioni.RicevutaMarginTopMm ?? 10;
        int marginRight = impostazioni.RicevutaMarginRightMm ?? 3;
        int marginBottom = impostazioni.RicevutaMarginBottomMm ?? 3;
        int marginLeft = impostazioni.RicevutaMarginLeftMm ?? 5;
        int larghezzaContenuto = Math.Max(10, larghezza - marginLeft - marginRight);
        string blocchettoLogo = string.IsNullOrWhiteSpace(logoBase64)
            ? string.Empty
            : $"<img src=\"{logoBase64}\" class=\"logo\" />";

        string rigaIndirizzo = string.IsNullOrWhiteSpace(indirizzoScuola) ? string.Empty : indirizzoScuola;
        string rigaPiva = string.IsNullOrWhiteSpace(pivaScuola) ? string.Empty : $"P.IVA/C.F.: {pivaScuola}";
        string subIntestazione = string.Join(" · ", new[] { rigaIndirizzo, rigaPiva }.Where(s => !string.IsNullOrWhiteSpace(s)));

        string titoloCortesia = a.Allievo?.Sesso switch
        {
            "F" => "Sig.ra",
            "M" => "Sig.",
            _ => ""
        };
        string nomeCompleto = a.Allievo?.NomeCompleto ?? $"{a.Allievo?.Nome} {a.Allievo?.Cognome}";
        string frasePagamento = string.IsNullOrWhiteSpace(titoloCortesia)
            ? $"{nomeCompleto} ha pagato € {a.ImportoPagato:N2}"
            : $"{titoloCortesia} {nomeCompleto} ha pagato € {a.ImportoPagato:N2}";

        string rigaOre = string.IsNullOrWhiteSpace(orario)
            ? string.Empty
            : $@"<div class=""riga""><span>Ore:</span><span>{orario}</span></div>";

        return $@"
<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8"" />
<title>Ricevuta di Cortesia</title>
<style>
    @page {{
    size: {larghezza}mm {altezza}mm;
    margin: {marginTop}mm {marginRight}mm {marginBottom}mm {marginLeft}mm;
}}
* {{ box-sizing: border-box; }}
body {{
    font-family: Arial, sans-serif;
    width: {larghezzaContenuto}mm;
    margin: 0;
    padding: 0;
    font-size: 9px;
    color: #111;
}}
    .logo {{
        display: block;
        max-width: 100%;
        max-height: 16mm;
        margin: 0 auto 2mm auto;
    }}
    .intestazione {{
        text-align: center;
        font-weight: bold;
        font-size: 11px;
        margin-bottom: 1mm;
    }}
    .intestazione-sub {{
        text-align: center;
        font-size: 7px;
        color: #444;
        margin-bottom: 2mm;
    }}
    .titolo {{
        text-align: center;
        font-weight: bold;
        font-size: 9px;
        border-top: 1px dashed #999;
        padding-top: 1.5mm;
        margin-bottom: 0.5mm;
    }}
    .sottotitolo {{
        text-align: center;
        font-size: 6px;
        color: #666;
        font-style: italic;
        border-bottom: 1px dashed #999;
        padding-bottom: 1.5mm;
        margin-bottom: 2mm;
    }}
    .pagamento {{
        text-align: center;
        font-weight: bold;
        font-size: 10px;
        margin-bottom: 2mm;
    }}
    .riga {{
        display: flex;
        justify-content: space-between;
        margin-bottom: 1mm;
        gap: 2mm;
    }}
    .riga span:first-child {{
        font-weight: bold;
        white-space: nowrap;
    }}
    .riga span:last-child {{
        text-align: right;
    }}
    .note {{
        text-align: center;
        font-size: 6px;
        color: #666;
        margin-top: 2mm;
        border-top: 1px dashed #999;
        padding-top: 2mm;
    }}
.stampa-btn {{
    display: block;
    width: 100%;
    margin-top: 3mm;
    padding: 6px 0;
    font-size: 10px;
    font-weight: bold;
    background-color: #4F46E5;
    color: white;
    border: none;
    border-radius: 6px;
    cursor: pointer;
}}
@media print {{
    .stampa-btn {{
        display: none;
    }}
}}
</style>
</head>
<body>
    {blocchettoLogo}
    <div class=""intestazione"">{nomeScuola}</div>
    {(string.IsNullOrWhiteSpace(subIntestazione) ? "" : $@"<div class=""intestazione-sub"">{subIntestazione}</div>")}

    <div class=""titolo"">RICEVUTA DI CORTESIA</div>
    <div class=""sottotitolo"">Valido solo come attestazione di pagamento</div>

    <div class=""pagamento"">{frasePagamento}</div>

    <div class=""riga""><span>Data:</span><span>{DateTime.Now:dd/MM/yyyy HH:mm}</span></div>
    <div class=""riga""><span>Corso:</span><span>{a.Corso?.Nome ?? "-"}</span></div>
    {rigaOre}
    <div class=""riga""><span>Tipologia:</span><span>{a.TipoAbbonamento}</span></div>
    <div class=""riga""><span>Periodo pagato:</span><span>{a.DataInizio:dd/MM/yyyy} - {a.DataScadenza:dd/MM/yyyy}</span></div>

    <div class=""note"">Documento non fiscale emesso a titolo di quietanza di pagamento.</div>
<button class=""stampa-btn"" onclick=""window.print()"">🖨️ Stampa</button>
</body>
</html>";
    }
}