using System.IO.Compression;
using System.Reflection;
using System.Text;
using Pulse.Models;

namespace Pulse.Services;

public class PrivacyDocumentService
{
    private const string NomeRisorsaModelloVuoto = "Pulse.Privacy.ModelloPrivacy.docx";
    private const string NomeRisorsaModelloAuto = "Pulse.Privacy.AutoModelloPrivacy.docx";

    private static byte[] LeggiRisorsa(string nomeRisorsa)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(nomeRisorsa);

        if (stream == null)
            throw new FileNotFoundException($"Risorsa privacy non trovata: {nomeRisorsa}. Verifica che il file sia in Privacy/ e impostato come EmbeddedResource nel .csproj.");

        using var memoria = new MemoryStream();
        stream.CopyTo(memoria);
        return memoria.ToArray();
    }

    // 📄 MODULO VUOTO: apre ModelloPrivacy.docx così com'è, nessuna sostituzione
    public async Task ApriModuloVuotoAsync()
    {
        try
        {
            byte[] bytes = LeggiRisorsa(NomeRisorsaModelloVuoto);
            string percorso = Path.Combine(FileSystem.CacheDirectory, $"ModuloPrivacy_Vuoto_{DateTime.Now:yyyyMMddHHmmss}.docx");

            await File.WriteAllBytesAsync(percorso, bytes);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Modulo Privacy (Vuoto)",
                File = new ShareFile(percorso)
            });
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Errore", $"Impossibile aprire il modulo privacy: {ex.Message}", "OK");
        }
    }

    // 📝 DOCUMENTO COMPILATO: legge AutoModelloPrivacy.docx e sostituisce i segnaposto {{...}}
    public async Task GeneraECondividiDocumentoCompilatoAsync(Allievi allievo)
    {
        try
        {
            byte[] bytes = LeggiRisorsa(NomeRisorsaModelloAuto);
            string nomeScuola = Preferences.Get("Scuola_Nome", "ASD SCUOLA DI DANZA PULSE");

            string dataNascitaTesto = allievo.DataNascita ?? string.Empty;
            if (DateTime.TryParse(allievo.DataNascita, out var dataNascitaParsata))
                dataNascitaTesto = dataNascitaParsata.ToString("dd/MM/yyyy");

            string telefonoTesto = !string.IsNullOrWhiteSpace(allievo.Telefono)
                ? allievo.Telefono
                : (allievo.Cellulare ?? string.Empty);

            var valori = new Dictionary<string, string>
            {
                ["{{NOME}}"] = allievo.Nome ?? string.Empty,
                ["{{COGNOME}}"] = allievo.Cognome ?? string.Empty,
                ["{{CODICEFISCALE}}"] = allievo.CodiceFiscale ?? string.Empty,
                ["{{DATANASCITA}}"] = dataNascitaTesto,
                ["{{SESSO}}"] = allievo.Sesso ?? string.Empty,
                ["{{INDIRIZZO}}"] = allievo.Indirizzo ?? string.Empty,
                ["{{CIVICO}}"] = allievo.NCivico ?? string.Empty,
                ["{{CAP}}"] = allievo.Cap ?? string.Empty,
                ["{{CITTA}}"] = allievo.Citta ?? string.Empty,
                ["{{PROVINCIA}}"] = allievo.Provincia ?? string.Empty,
                ["{{TELEFONO}}"] = telefonoTesto,
                ["{{EMAIL}}"] = allievo.Email ?? string.Empty,
                ["{{DATA_OGGI}}"] = DateTime.Now.ToString("dd/MM/yyyy"),
                ["{{NOME_SCUOLA}}"] = nomeScuola
            };

            byte[] compilato = SostituisciSegnapostiNelDocx(bytes, valori);

            string percorso = Path.Combine(FileSystem.CacheDirectory, "AutoModelloPrivacy.docx");
            await File.WriteAllBytesAsync(percorso, compilato);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Modulo Privacy Compilato",
                File = new ShareFile(percorso)
            });
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Errore", $"Impossibile generare il modulo privacy compilato: {ex.Message}", "OK");
        }
    }

    private static byte[] SostituisciSegnapostiNelDocx(byte[] templateBytes, Dictionary<string, string> valori)
    {
        using var memoria = new MemoryStream();
        memoria.Write(templateBytes, 0, templateBytes.Length);
        memoria.Position = 0;

        using (var archivio = new ZipArchive(memoria, ZipArchiveMode.Update, leaveOpen: true))
        {
            var voceDocumento = archivio.GetEntry("word/document.xml");
            if (voceDocumento == null)
                throw new InvalidOperationException("Il file .docx non è valido: manca word/document.xml.");

            string xml;
            using (var reader = new StreamReader(voceDocumento.Open(), Encoding.UTF8))
            {
                xml = reader.ReadToEnd();
            }

            foreach (var coppia in valori)
            {
                xml = xml.Replace(coppia.Key, EscapeXml(coppia.Value));
            }

            voceDocumento.Delete();
            var nuovaVoce = archivio.CreateEntry("word/document.xml");
            using var writer = new StreamWriter(nuovaVoce.Open(), new UTF8Encoding(false));
            writer.Write(xml);
        }

        return memoria.ToArray();
    }

    private static string EscapeXml(string testo) =>
        testo
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
}