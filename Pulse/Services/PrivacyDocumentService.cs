using System.IO.Compression;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Pulse.DTO;
using Pulse.Models;

namespace Pulse.Services;

public class PrivacyDocumentService
{
    private const string NomeRisorsaModelloVuoto = "Pulse.Privacy.ModelloPrivacy.docx";
    private const string NomeRisorsaModelloAuto = "Pulse.Privacy.AutoModelloPrivacy.docx";

    private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

    private readonly IImpostazioniService _impostazioniService;
    private readonly IStampaService _stampaService;
    private readonly IDatabaseService _databaseService;

    public PrivacyDocumentService(IImpostazioniService impostazioniService, IStampaService stampaService, IDatabaseService databaseService)
    {
        _impostazioniService = impostazioniService;
        _stampaService = stampaService;
        _databaseService = databaseService;
    }

    // ================================================
    // MODELLO HTML DELLA SCUOLA
    // ================================================
    // Sta FUORI dall'applicazione, accanto al database: serve una build sola per
    // tutte le scuole, e il modulo non viene sovrascritto dagli aggiornamenti,
    // che rimpiazzano la cartella d'installazione.
    //
    // Due modelli, scelti dalla spunta "Dispositivo" nelle impostazioni:
    //   ModuloPrivacy.html            -> a penna: le domande extra sono caselle
    //                                    disegnate, si barrano a mano (approccio ibrido)
    //   ModuloPrivacyDispositivo.html -> con i campi {{EXTRA:...}}, che il tablet
    //                                    fa compilare all'allievo
    // Ognuno dei due serve sia per il modulo vuoto sia per il compilato: il vuoto si
    // ottiene sostituendo i segnaposto con righe puntinate.

    public const string NomeFileModelloHtml = "ModuloPrivacy.html";
    public const string NomeFileModelloDispositivo = "ModuloPrivacyDispositivo.html";

    public static string CartellaModelli => Path.Combine(FileSystem.AppDataDirectory, "Privacy");

    public static string PercorsoModelloHtml => Path.Combine(CartellaModelli, NomeFileModelloHtml);

    public static string PercorsoModelloDispositivo => Path.Combine(CartellaModelli, NomeFileModelloDispositivo);

    /// <summary>
    /// Vero se c'è almeno uno dei due modelli HTML.
    /// Verificato a ogni chiamata: il file può comparire mentre l'app è aperta.
    /// </summary>
    public static bool ModelloHtmlDisponibile =>
        File.Exists(PercorsoModelloHtml) || File.Exists(PercorsoModelloDispositivo);

    /// <summary>
    /// Sceglie il modello in base alla spunta "Dispositivo". Se manca quello
    /// richiesto si usa l'altro, così la stampa non si blocca mai per un file assente.
    /// </summary>
    /// <returns>Percorso del modello da usare, oppure null se non ce n'è nessuno.</returns>
    public async Task<string?> ScegliModelloHtmlAsync()
    {
        var impostazioni = await _impostazioniService.GetImpostazioniAsync();
        bool dispositivo = impostazioni.DispositivoPrivacy == 1;

        string preferito = dispositivo ? PercorsoModelloDispositivo : PercorsoModelloHtml;
        string alternativo = dispositivo ? PercorsoModelloHtml : PercorsoModelloDispositivo;

        if (File.Exists(preferito)) return preferito;
        if (File.Exists(alternativo)) return alternativo;
        return null;
    }

    /// <summary>Nome del file che verrebbe usato adesso: si registra sulla riga Privacy.</summary>
    public async Task<string> NomeModelloInUsoAsync()
    {
        string? percorso = await ScegliModelloHtmlAsync();
        return percorso != null ? Path.GetFileName(percorso) : "AutoModelloPrivacy.docx";
    }

    // ================================================
    // DISPONIBILITÀ DEI MODELLI
    // ================================================
    // I modelli .docx restano supportati per chi preferisce Word: se c'è l'HTML
    // vince quello, altrimenti si usa il .docx esattamente come prima.

    private static readonly bool _modelloVuotoPresente = RisorsaPresente(NomeRisorsaModelloVuoto);
    private static readonly bool _modelloAutoPresente = RisorsaPresente(NomeRisorsaModelloAuto);

    public static bool ModelloVuotoDisponibile => ModelloHtmlDisponibile || _modelloVuotoPresente;
    public static bool ModelloCompilatoDisponibile => ModelloHtmlDisponibile || _modelloAutoPresente;
    public static bool AlmenoUnModelloDisponibile => ModelloVuotoDisponibile || ModelloCompilatoDisponibile;

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
        // Il modello HTML della scuola ha la precedenza sul .docx.
        if (ModelloHtmlDisponibile)
        {
            await StampaModuloHtmlAsync(allievo: null, compilato: false);
            return;
        }

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
        // Il modello HTML della scuola ha la precedenza sul .docx.
        if (ModelloHtmlDisponibile)
        {
            await StampaModuloHtmlAsync(allievo, compilato: true);
            return;
        }

        if (!ModelloCompilatoDisponibile)
        {
            await AvvisaModelloMancanteAsync("AutoModelloPrivacy.docx");
            return;
        }

        try
        {
            byte[] bytes = LeggiRisorsa(NomeRisorsaModelloAuto);

            // Stessi segnaposto del percorso HTML: così il LEGGIMI vale per entrambi
            // e i due meccanismi non possono divergere.
            var valori = new Dictionary<string, string>(CostruisciValoriAllievo(allievo));
            foreach (var coppia in await CostruisciValoriScuolaAsync())
            {
                // Il logo nel .docx non si può iniettare come tag HTML.
                if (coppia.Key == "{{LOGO}}") continue;
                valori[coppia.Key] = coppia.Value;
            }

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

    // ================================================
    // PREPARAZIONE DELLA CARTELLA MODELLI
    // ================================================

    /// <summary>
    /// Crea la cartella dei modelli con le istruzioni e un esempio, se non c'è già.
    /// Non sovrascrive mai un file esistente: il modello della scuola è intoccabile.
    /// </summary>
    /// <returns>Percorso della cartella.</returns>
    public static async Task<string> PreparaCartellaModelliAsync()
    {
        Directory.CreateDirectory(CartellaModelli);

        // Il LEGGIMI è documentazione di Pulse, non della scuola: si riscrive sempre,
        // così dopo un aggiornamento spiega le funzioni della versione installata.
        // I modelli della scuola invece non vengono mai toccati.
        string percorsoLeggimi = Path.Combine(CartellaModelli, "LEGGIMI.txt");
        await File.WriteAllTextAsync(percorsoLeggimi, TestoLeggimiHtml);

        string percorsoEsempio = Path.Combine(CartellaModelli, "ModuloPrivacy_ESEMPIO.html");
        if (!File.Exists(percorsoEsempio))
        {
            await File.WriteAllTextAsync(percorsoEsempio, ModelloEsempio);
        }

        return CartellaModelli;
    }

    /// <summary>Apre la cartella dei modelli in Esplora file, creandola se serve.</summary>
    public static async Task ApriCartellaModelliAsync()
    {
        try
        {
            string cartella = await PreparaCartellaModelliAsync();
            await Launcher.Default.OpenAsync(new Uri($"file:///{cartella.Replace('\\', '/')}"));
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Errore", $"Impossibile aprire la cartella dei modelli: {ex.Message}", "OK");
        }
    }

    // ================================================
    // PERCORSO HTML
    // ================================================

    /// <summary>
    /// Valori della scuola: compaiono sia nel modulo compilato sia in quello vuoto,
    /// perché non sono dati da far scrivere a mano all'allievo.
    /// </summary>
    private async Task<Dictionary<string, string>> CostruisciValoriScuolaAsync()
    {
        var impostazioni = await _impostazioniService.GetImpostazioniAsync();

        string nomeScuola = string.IsNullOrWhiteSpace(impostazioni.NomeScuola)
            ? "ASD SCUOLA DI DANZA PULSE"
            : impostazioni.NomeScuola;

        string logo = string.Empty;
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
            logo = $"<img src='data:{mime};base64,{Convert.ToBase64String(bytes)}' style='max-height:20mm' />";
        }

        return new Dictionary<string, string>
        {
            ["{{NOME_SCUOLA}}"] = nomeScuola,
            ["{{INDIRIZZO_SCUOLA}}"] = impostazioni.IndirizzoScuola ?? string.Empty,
            ["{{PIVA_SCUOLA}}"] = impostazioni.PartitaIva ?? string.Empty,
            ["{{LOGO}}"] = logo,
            ["{{DATA_OGGI}}"] = DateTime.Now.ToString("dd/MM/yyyy")
        };
    }

    /// <summary>
    /// Valori dell'allievo. Nel modulo vuoto questi diventano righe puntinate
    /// da riempire a penna.
    /// </summary>
    private static Dictionary<string, string> CostruisciValoriAllievo(Allievi? allievo)
    {
        string dataNascitaTesto = allievo?.DataNascita ?? string.Empty;
        if (DateTime.TryParse(allievo?.DataNascita, out var dataNascitaParsata))
            dataNascitaTesto = dataNascitaParsata.ToString("dd/MM/yyyy");

        string telefonoTesto = !string.IsNullOrWhiteSpace(allievo?.Telefono)
            ? allievo!.Telefono!
            : (allievo?.Cellulare ?? string.Empty);

        return new Dictionary<string, string>
        {
            ["{{NOME}}"] = allievo?.Nome ?? string.Empty,
            ["{{COGNOME}}"] = allievo?.Cognome ?? string.Empty,
            ["{{CODICEFISCALE}}"] = allievo?.CodiceFiscale ?? string.Empty,
            ["{{DATANASCITA}}"] = dataNascitaTesto,
            ["{{SESSO}}"] = allievo?.Sesso ?? string.Empty,
            ["{{INDIRIZZO}}"] = allievo?.Indirizzo ?? string.Empty,
            ["{{CIVICO}}"] = allievo?.NCivico ?? string.Empty,
            ["{{CAP}}"] = allievo?.Cap ?? string.Empty,
            ["{{CITTA}}"] = allievo?.Citta ?? string.Empty,
            ["{{PROVINCIA}}"] = allievo?.Provincia ?? string.Empty,
            ["{{TELEFONO}}"] = telefonoTesto,
            ["{{CELLULARE}}"] = allievo?.Cellulare ?? string.Empty,
            ["{{EMAIL}}"] = allievo?.Email ?? string.Empty
        };
    }

    /// <summary>
    /// Riga puntinata usata nel modulo vuoto al posto dei dati dell'allievo.
    /// Stile in linea, così funziona anche nei modelli che non hanno un CSS nostro.
    /// </summary>
    private static string RigaPuntinata(string segnaposto)
    {
        // Campi corti restano corti, altrimenti il modulo si sfalsa.
        int larghezzaMm = segnaposto switch
        {
            "{{CAP}}" or "{{PROVINCIA}}" or "{{SESSO}}" or "{{CIVICO}}" => 18,
            "{{DATANASCITA}}" => 30,
            _ => 55
        };

        return $"<span style='display:inline-block;min-width:{larghezzaMm}mm;border-bottom:1px dotted #333'>&nbsp;</span>";
    }

    /// <summary>
    /// Genera l'HTML del modulo dal modello della scuola.
    /// </summary>
    public async Task<string> GeneraHtmlModuloAsync(Allievi? allievo, bool compilato)
    {
        string percorsoModello = await ScegliModelloHtmlAsync()
            ?? throw new FileNotFoundException("Nessun modello HTML presente nella cartella Privacy.");

        string modello = await File.ReadAllTextAsync(percorsoModello);

        foreach (var coppia in await CostruisciValoriScuolaAsync())
        {
            modello = modello.Replace(coppia.Key, coppia.Value);
        }

        foreach (var coppia in CostruisciValoriAllievo(allievo))
        {
            string valore = compilato ? coppia.Value : RigaPuntinata(coppia.Key);

            // Anche nel modulo compilato, un campo vuoto diventa una riga da
            // riempire a penna: meglio uno spazio da scrivere che un buco.
            if (compilato && string.IsNullOrWhiteSpace(coppia.Value))
                valore = RigaPuntinata(coppia.Key);

            modello = modello.Replace(coppia.Key, valore);
        }

        modello = await SostituisciCampiExtraAsync(modello, allievo, compilato);

        return modello;
    }

    // ================================================
    // CAMPI EXTRA — {{EXTRA:CHIAVE|Etichetta|tipo}}
    // ================================================

    private static readonly Regex RegexCampoExtra = new(
        @"\{\{EXTRA:([A-Z0-9_]+)(?:\|([^|}]*))?(?:\|([^}]*))?\}\}",
        RegexOptions.Compiled);

    /// <summary>
    /// Domande extra che il tablet deve fare all'allievo.
    ///
    /// Si leggono SOLO dal modello dispositivo. Se quel file non c'è la lista è
    /// vuota, e il tablet chiede soltanto i dati anagrafici: così il dispositivo
    /// funziona anche per le scuole che non hanno nessun modulo privacy.
    /// </summary>
    public static List<CampoExtraDTO> LeggiCampiExtraPerDispositivo()
    {
        if (!File.Exists(PercorsoModelloDispositivo)) return new List<CampoExtraDTO>();

        try
        {
            return LeggiCampiExtra(File.ReadAllText(PercorsoModelloDispositivo));
        }
        catch
        {
            return new List<CampoExtraDTO>();
        }
    }

    /// <summary>Estrae i campi {{EXTRA:...}} da un testo HTML.</summary>
    private static List<CampoExtraDTO> LeggiCampiExtra(string modello)
    {
        var campi = new List<CampoExtraDTO>();

        foreach (Match m in RegexCampoExtra.Matches(modello))
        {
            string chiave = m.Groups[1].Value;
            if (campi.Any(c => c.Chiave == chiave)) continue;

            string etichetta = m.Groups[2].Success && !string.IsNullOrWhiteSpace(m.Groups[2].Value)
                ? m.Groups[2].Value.Trim()
                : chiave;

            var opzioni = new List<string>();
            string tipo = m.Groups[3].Success ? m.Groups[3].Value.Trim() : string.Empty;

            if (tipo.StartsWith("scelta:", StringComparison.OrdinalIgnoreCase))
            {
                opzioni = tipo["scelta:".Length..]
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();
            }

            campi.Add(new CampoExtraDTO
            {
                Chiave = chiave,
                Etichetta = etichetta,
                Opzioni = opzioni,
                Segnaposto = m.Value
            });
        }

        return campi;
    }

    private async Task<string> SostituisciCampiExtraAsync(string modello, Allievi? allievo, bool compilato)
    {
        // Si leggono dal modello che si sta stampando: in quello a penna non ce ne
        // sono, e la sostituzione non fa nulla.
        var campi = LeggiCampiExtra(modello);
        if (campi.Count == 0) return modello;

        // Le risposte esistono solo per un allievo già a database.
        var risposte = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (compilato && allievo is { Id: > 0 })
        {
            foreach (var riga in await _databaseService.GetCampiExtraAllievoAsync(allievo.Id))
            {
                risposte[riga.Chiave] = riga.Valore ?? string.Empty;
            }
        }

        foreach (var campo in campi)
        {
            risposte.TryGetValue(campo.Chiave, out string? valore);

            string html = campo.IsScelta
                ? RenderScelta(campo, valore)
                : RenderTestoLibero(valore);

            modello = modello.Replace(campo.Segnaposto, html);
        }

        return modello;
    }

    /// <summary>Caselle da barrare: già segnata quella scelta dall'allievo, se c'è.</summary>
    private static string RenderScelta(CampoExtraDTO campo, string? valore)
    {
        var pezzi = campo.Opzioni.Select(opzione =>
        {
            bool scelta = !string.IsNullOrWhiteSpace(valore)
                          && string.Equals(opzione, valore, StringComparison.OrdinalIgnoreCase);

            string segno = scelta ? "&#10007;" : "&nbsp;";

            return "<span style='display:inline-block;margin-right:6mm;white-space:nowrap'>" +
                   $"{opzione} <span style='display:inline-block;width:5mm;height:5mm;border:1px solid #000;" +
                   $"text-align:center;line-height:5mm;vertical-align:-1mm'>{segno}</span></span>";
        });

        return string.Concat(pezzi);
    }

    /// <summary>Testo libero: il valore scritto, oppure una riga da riempire a penna.</summary>
    private static string RenderTestoLibero(string? valore)
    {
        return string.IsNullOrWhiteSpace(valore)
            ? "<span style='display:inline-block;min-width:55mm;border-bottom:1px dotted #333'>&nbsp;</span>"
            : System.Net.WebUtility.HtmlEncode(valore);
    }

    /// <summary>
    /// Genera e manda in stampa il modulo. Segue il flag "Stampa diretta" delle
    /// impostazioni: se è spento si apre l'anteprima nel visualizzatore.
    /// </summary>
    private async Task StampaModuloHtmlAsync(Allievi? allievo, bool compilato)
    {
        try
        {
            string html = await GeneraHtmlModuloAsync(allievo, compilato);

            var impostazioni = await _impostazioniService.GetImpostazioniAsync();
            bool silenziosa = impostazioni.StampaSilenziosa == 1;

            if (silenziosa && _stampaService.SupportaStampaSilenziosa)
            {
                bool stampato = await _stampaService.StampaHtmlAsync(html);
                if (stampato) return;

                // Stampa diretta non riuscita: si prosegue con l'anteprima.
            }

            string nomeFile = compilato
                ? $"ModuloPrivacy_{PulisciNomeFile($"{allievo?.Cognome}_{allievo?.Nome}")}_{DateTime.Now:yyyyMMddHHmmss}.html"
                : $"ModuloPrivacy_Vuoto_{DateTime.Now:yyyyMMddHHmmss}.html";

            string percorso = Path.Combine(FileSystem.CacheDirectory, nomeFile);
            await File.WriteAllTextAsync(percorso, html);

            await Launcher.Default.OpenAsync(new OpenFileRequest
            {
                Title = compilato ? "Modulo Privacy Compilato" : "Modulo Privacy (Vuoto)",
                File = new ReadOnlyFile(percorso)
            });
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Errore", $"Impossibile generare il modulo privacy: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Archivia il modulo firmato come PDF in cartella\anno\.
    /// Va chiamato quando la segreteria spunta "Firmato": prima di quel momento
    /// esiste solo la carta, e non c'è niente da conservare.
    /// </summary>
    /// <returns>Percorso del PDF creato, oppure null se non è stato possibile.</returns>
    public async Task<string?> ArchiviaModuloFirmatoAsync(Allievi allievo, string? cartellaArchivio = null)
    {
        try
        {
            if (!ModelloHtmlDisponibile) return null;
            if (!_stampaService.SupportaStampaSilenziosa) return null;

            var impostazioni = await _impostazioniService.GetImpostazioniAsync();

            string cartella = cartellaArchivio ?? impostazioni.CartellaModuliPath ?? string.Empty;
            if (string.IsNullOrWhiteSpace(cartella)) return null;

            string html = await GeneraHtmlModuloAsync(allievo, compilato: true);

            string anno = DateTime.Now.Year.ToString();
            string nomeFile = $"Privacy_{PulisciNomeFile($"{allievo.Cognome}_{allievo.Nome}")}_{DateTime.Now:yyyyMMdd}.pdf";
            string percorsoPdf = Path.Combine(cartella, anno, nomeFile);

            bool creato = await _stampaService.SalvaHtmlComePdfAsync(html, percorsoPdf);
            return creato ? percorsoPdf : null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PrivacyDocumentService] Archiviazione fallita: {ex.Message}");
            return null;
        }
    }

    // ================================================
    // CONTENUTI CREATI AL PRIMO AVVIO
    // ================================================

    private const string TestoLeggimiHtml = @"================================================================================
          MODULO PRIVACY IN HTML — ISTRUZIONI
================================================================================

In questa cartella va il modulo privacy della scuola, in formato HTML.
Questo file LEGGIMI viene riscritto da Pulse a ogni aggiornamento: non
modificarlo, le tue note mettile in un altro file.

1. I DUE MODELLI

   ModuloPrivacy.html
       Modulo da compilare A PENNA (approccio ibrido). Le domande in piu'
       (professione, come ci hai conosciuto, ...) sono caselle e righe disegnate
       normalmente in HTML, e si barrano a mano.

   ModuloPrivacyDispositivo.html
       Modulo usato quando in Impostazioni > Documento Privacy e' attiva la
       spunta 'Dispositivo'. Le domande in piu' sono campi {{EXTRA:...}} che
       l'allievo compila sul tablet (vedi punto 3).

   Quale usare lo decide la spunta 'Dispositivo'. Se manca quello richiesto,
   Pulse usa l'altro; se mancano entrambi, usa i modelli Word (.docx).

   Non serve metterli tutti e due: basta quello che la scuola usa davvero.

   Ogni modello serve sia per il modulo compilato (i segnaposto diventano i
   dati dell'allievo) sia per il modulo vuoto (i segnaposto diventano righe
   puntinate da riempire a penna).

   In questa cartella trovi ModuloPrivacy_ESEMPIO.html come punto di partenza:
   copialo, rinominalo e adattalo.

2. SEGNAPOSTO DISPONIBILI

   Dati dell'allievo (puntinati nel modulo vuoto):
   {{NOME}} {{COGNOME}} {{CODICEFISCALE}} {{DATANASCITA}} {{SESSO}}
   {{INDIRIZZO}} {{CIVICO}} {{CAP}} {{CITTA}} {{PROVINCIA}}
   {{TELEFONO}} {{CELLULARE}} {{EMAIL}}

   Dati della scuola (sempre compilati, anche nel modulo vuoto):
   {{NOME_SCUOLA}} {{INDIRIZZO_SCUOLA}} {{PIVA_SCUOLA}} {{DATA_OGGI}} {{LOGO}}

   {{LOGO}} diventa l'immagine impostata nelle Impostazioni. Se non c'e' logo,
   sparisce senza lasciare spazi vuoti.

3. CAMPI EXTRA — SOLO IN ModuloPrivacyDispositivo.html

   Sono le domande che il tablet fa all'allievo, e le risposte finiscono in
   archivio e nelle statistiche.

   {{EXTRA:PROFESSIONE|Professione|testo}}
   {{EXTRA:CONOSCENZA|Come ci hai conosciuto|scelta:RADIO,FACEBOOK,VOLANTINO,AMICO}}
   {{EXTRA:TAGLIA}}                          (etichetta = chiave, testo libero)

   - La CHIAVE (prima parte) va in MAIUSCOLO, senza spazi: e' con quella che le
     risposte vengono archiviate. Non cambiarla piu' una volta che la scuola ha
     iniziato a raccogliere risposte, o le vecchie restano sotto il nome vecchio.
   - L'ETICHETTA e' il testo che legge l'allievo: si puo' cambiare quando vuoi.
   - Con 'scelta:' escono caselle da barrare, una per opzione, separate da
     virgola. Senza, esce una riga da riempire.

   Il tablet funziona anche SENZA questo file: in quel caso chiede solo i dati
   anagrafici, crea l'allievo e lo lascia alla segreteria per l'abbonamento.

4. CONSIGLI PER LA STAMPA

   - Imposta il formato foglio nel CSS:  @page { size: A4; margin: 15mm; }
   - Usa millimetri (mm) per le misure, non pixel.
   - Tutto deve stare nel file: niente immagini o fogli di stile esterni,
     perche' il modulo viene stampato senza connessione.

5. SE PREFERISCI WORD

   Non mettere nessun modello HTML qui: Pulse continuera' a usare i modelli
   .docx come prima. Se c'e' un HTML, ha la precedenza.

6. AGGIORNAMENTI

   Questa cartella sta fuori dall'applicazione, quindi gli aggiornamenti di
   Pulse non toccano i modelli della scuola.

================================================================================
";

    private const string ModelloEsempio = @"<!DOCTYPE html>
<html lang='it'>
<head>
<meta charset='utf-8' />
<title>Informativa e consenso privacy</title>
<style>
    @page { size: A4; margin: 15mm; }
    body { font-family: Arial, Helvetica, sans-serif; font-size: 11pt; color: #111; }
    .intestazione { text-align: center; margin-bottom: 8mm; }
    .intestazione h1 { font-size: 14pt; margin: 2mm 0; }
    .intestazione .dati { font-size: 9pt; color: #444; }
    h2 { font-size: 12pt; border-bottom: 1px solid #999; padding-bottom: 1mm; margin-top: 7mm; }
    .campo { margin-bottom: 3mm; }
    .campo .etichetta { font-weight: bold; }
    .due-colonne { display: flex; gap: 8mm; }
    .due-colonne > div { flex: 1; }
    .testo { text-align: justify; font-size: 10pt; line-height: 1.5; }
    .firma { margin-top: 12mm; display: flex; justify-content: space-between; }
    .firma div { width: 45%; border-top: 1px solid #333; padding-top: 2mm; font-size: 9pt; }
</style>
</head>
<body>

<div class='intestazione'>
    {{LOGO}}
    <h1>{{NOME_SCUOLA}}</h1>
    <div class='dati'>{{INDIRIZZO_SCUOLA}} — P.IVA/C.F. {{PIVA_SCUOLA}}</div>
</div>

<h2>Dati dell'interessato</h2>

<div class='due-colonne'>
    <div>
        <div class='campo'><span class='etichetta'>Nome:</span> {{NOME}}</div>
        <div class='campo'><span class='etichetta'>Data di nascita:</span> {{DATANASCITA}}</div>
        <div class='campo'><span class='etichetta'>Indirizzo:</span> {{INDIRIZZO}} {{CIVICO}}</div>
        <div class='campo'><span class='etichetta'>Telefono:</span> {{TELEFONO}}</div>
    </div>
    <div>
        <div class='campo'><span class='etichetta'>Cognome:</span> {{COGNOME}}</div>
        <div class='campo'><span class='etichetta'>Codice fiscale:</span> {{CODICEFISCALE}}</div>
        <div class='campo'><span class='etichetta'>Citta':</span> {{CITTA}} ({{PROVINCIA}}) — {{CAP}}</div>
        <div class='campo'><span class='etichetta'>Email:</span> {{EMAIL}}</div>
    </div>
</div>

<h2>Informativa</h2>

<p class='testo'>
    SOSTITUISCI QUESTO TESTO CON L'INFORMATIVA DELLA TUA SCUOLA.
    Questo file e' solo un punto di partenza: il contenuto legale va definito
    dalla scuola o dal suo consulente. Pulse si limita a compilare i dati e a
    stampare il documento.
</p>

<h2>Consenso</h2>

<p class='testo'>
    Il/La sottoscritto/a, letta l'informativa, presta il proprio consenso al
    trattamento dei dati personali per le finalita' sopra indicate.
</p>

<div class='firma'>
    <div>Luogo e data: {{DATA_OGGI}}</div>
    <div>Firma dell'interessato</div>
</div>

</body>
</html>
";

    private static string PulisciNomeFile(string testo)
    {
        foreach (char carattereNonValido in Path.GetInvalidFileNameChars())
        {
            testo = testo.Replace(carattereNonValido, '_');
        }

        return testo.Trim('_').Replace(' ', '_');
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