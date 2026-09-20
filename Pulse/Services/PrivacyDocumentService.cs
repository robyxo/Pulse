using System.IO.Compression;
using System.Reflection;
using System.Xml.Linq;
using Pulse.Models;

namespace Pulse.Services;

public class PrivacyDocumentService
{
    private const string NomeRisorsaModelloVuoto = "Pulse.Privacy.ModelloPrivacy.docx";
    private const string NomeRisorsaModelloAuto = "Pulse.Privacy.AutoModelloPrivacy.docx";

    private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

    private readonly IImpostazioniService _impostazioniService;

    public PrivacyDocumentService(IImpostazioniService impostazioniService)
    {
        _impostazioniService = impostazioniService;
    }

    // ================================================
    // DISPONIBILITÀ DEI MODELLI
    // ================================================
    // I modelli .docx sono opzionali: ogni scuola mette i propri in Pulse/Privacy/
    // prima di compilare (vedi LEGGIMI.txt). Se non ci sono, le funzioni privacy
    // restano nascoste invece di dare errore.

    private static readonly bool _modelloVuotoPresente = RisorsaPresente(NomeRisorsaModelloVuoto);
    private static readonly bool _modelloAutoPresente = RisorsaPresente(NomeRisorsaModelloAuto);

    public static bool ModelloVuotoDisponibile => _modelloVuotoPresente;
    public static bool ModelloCompilatoDisponibile => _modelloAutoPresente;
    public static bool AlmenoUnModelloDisponibile => _modelloVuotoPresente || _modelloAutoPresente;

    private static bool RisorsaPresente(string nomeRisorsa)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(nomeRisorsa);
        return stream != null;
    }

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

    private static Task AvvisaModelloMancanteAsync(string nomeFile) =>
        Shell.Current.DisplayAlert(
            "Modello non presente",
            $"Nella cartella Privacy dell'applicazione non è stato inserito il file {nomeFile}.\n\nSegui le istruzioni del file LEGGIMI.txt per aggiungere il modulo della tua scuola.",
            "OK");

    public async Task ApriModuloVuotoAsync()
    {
        if (!ModelloVuotoDisponibile)
        {
            await AvvisaModelloMancanteAsync("ModelloPrivacy.docx");
            return;
        }

        try
        {
            byte[] bytes = LeggiRisorsa(NomeRisorsaModelloVuoto);
            string percorso = Path.Combine(FileSystem.CacheDirectory, $"ModuloPrivacy_Vuoto_{DateTime.Now:yyyyMMddHHmmss}.docx");
            await File.WriteAllBytesAsync(percorso, bytes);

            await Launcher.Default.OpenAsync(new OpenFileRequest
            {
                Title = "Modulo Privacy (Vuoto)",
                File = new ReadOnlyFile(percorso)
            });
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Errore", $"Impossibile aprire il modulo privacy: {ex.Message}", "OK");
        }
    }

    public async Task GeneraECondividiDocumentoCompilatoAsync(Allievi allievo)
    {
        if (!ModelloCompilatoDisponibile)
        {
            await AvvisaModelloMancanteAsync("AutoModelloPrivacy.docx");
            return;
        }

        try
        {
            byte[] bytes = LeggiRisorsa(NomeRisorsaModelloAuto);

            var impostazioni = await _impostazioniService.GetImpostazioniAsync();
            string nomeScuola = string.IsNullOrWhiteSpace(impostazioni.NomeScuola)
                ? "ASD SCUOLA DI DANZA PULSE"
                : impostazioni.NomeScuola;

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
                ["{{CELLULARE}}"] = allievo.Cellulare ?? string.Empty,
                ["{{EMAIL}}"] = allievo.Email ?? string.Empty,
                ["{{DATA_OGGI}}"] = DateTime.Now.ToString("dd/MM/yyyy"),
                ["{{NOME_SCUOLA}}"] = nomeScuola
            };

            byte[] compilato = SostituisciSegnapostiNelDocx(bytes, valori);
            string percorso = Path.Combine(FileSystem.CacheDirectory, "AutoModelloPrivacy.docx");
            await File.WriteAllBytesAsync(percorso, compilato);

            await Launcher.Default.OpenAsync(new OpenFileRequest
            {
                Title = "Modulo Privacy Compilato",
                File = new ReadOnlyFile(percorso)
            });
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Errore", $"Impossibile generare il modulo privacy compilato: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Sostituisce i segnaposto {{TAG}} nel documento, normalizzando il testo
    /// a livello di paragrafo. Serve perché Word spesso spezza un segnaposto
    /// su più "run" XML interni (autocorrezione, correttore ortografico, ecc.):
    /// una sostituzione a stringa semplice su document.xml grezzo non li trova
    /// se sono spezzati. Unendo tutto il testo del paragrafo, sostituendo, e
    /// rimettendolo nel primo run, la sostituzione funziona indipendentemente
    /// da come Word ha spezzato i run.
    /// </summary>
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

            XDocument documento;
            using (var stream = voceDocumento.Open())
            {
                documento = XDocument.Load(stream);
            }

            foreach (var paragrafo in documento.Descendants(W + "p"))
            {
                var nodiTesto = paragrafo.Descendants(W + "t").ToList();
                if (nodiTesto.Count == 0)
                    continue;

                string testoCompleto = string.Concat(nodiTesto.Select(n => n.Value));
                string testoSostituito = testoCompleto;

                foreach (var coppia in valori)
                    testoSostituito = testoSostituito.Replace(coppia.Key, coppia.Value ?? string.Empty);

                if (testoSostituito == testoCompleto)
                    continue;

                nodiTesto[0].Value = testoSostituito;
                for (int i = 1; i < nodiTesto.Count; i++)
                    nodiTesto[i].Value = string.Empty;
            }

            voceDocumento.Delete();
            var nuovaVoce = archivio.CreateEntry("word/document.xml");
            using (var writer = nuovaVoce.Open())
            {
                documento.Save(writer, SaveOptions.DisableFormatting);
            }
        }

        return memoria.ToArray();
    }
}