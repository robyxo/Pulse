using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Pulse.DTO;
using Pulse.Models;

namespace Pulse.Services;

/// <summary>
/// Implementazione di <see cref="IServerDispositivoService"/>.
///
/// Usa un TcpListener e non HttpListener di proposito: HttpListener su Windows,
/// per rispondere ad altri dispositivi della rete, richiede i permessi di
/// amministratore (netsh http add urlacl). Il TcpListener no: basta dire
/// "Consenti" alla richiesta del firewall di Windows la prima volta.
///
/// Il protocollo HTTP gestito è il minimo indispensabile: una richiesta per
/// connessione, GET per la pagina e POST con JSON per la registrazione.
/// </summary>
public class ServerDispositivoService : IServerDispositivoService
{
    /// <summary>Porta usata se nelle impostazioni non ce n'è una.</summary>
    public const int PortaPredefinita = 5000;

    private const int DimensioneMassimaIntestazioni = 16 * 1024;
    private const int DimensioneMassimaCorpo = 64 * 1024;
    private static readonly TimeSpan TempoMassimoRichiesta = TimeSpan.FromSeconds(15);

    private const string PercorsoConPrivacy = "registrazionePrivacy";
    private const string PercorsoSenzaPrivacy = "registrazione";

    private static readonly JsonSerializerOptions OpzioniJson = new() { PropertyNameCaseInsensitive = true };

    private readonly IDatabaseService _databaseService;
    private readonly IImpostazioniService _impostazioniService;
    private readonly IAvvisiDispositivoService _avvisiDispositivoService;

    // Evita due avvii contemporanei (es. avvio app + salvataggio impostazioni).
    private readonly SemaphoreSlim _bloccoAvvio = new(1, 1);

    // Controllo del codice fiscale e salvataggio vanno fatti insieme: due invii
    // ravvicinati (doppio tocco sul bottone) creerebbero due allievi uguali.
    private readonly SemaphoreSlim _bloccoRegistrazione = new(1, 1);

    private TcpListener? _listener;
    private CancellationTokenSource? _annullamento;

    public ServerDispositivoService(
        IDatabaseService databaseService,
        IImpostazioniService impostazioniService,
        IAvvisiDispositivoService avvisiDispositivoService)
    {
        _databaseService = databaseService;
        _impostazioniService = impostazioniService;
        _avvisiDispositivoService = avvisiDispositivoService;
    }

    public bool InAscolto => _listener != null;

    public int Porta { get; private set; } = PortaPredefinita;

    public string? UltimoErrore { get; private set; }

    // ------------------------------------------------------------------
    // ACCENSIONE E SPEGNIMENTO
    // ------------------------------------------------------------------

    public async Task AggiornaDaImpostazioniAsync()
    {
        await _bloccoAvvio.WaitAsync();
        try
        {
            var impostazioni = await _impostazioniService.GetImpostazioniAsync();

            bool attivo = impostazioni.DispositivoPrivacy == 1;
            int porta = impostazioni.ApiTabletPorta is > 0 and < 65536
                ? impostazioni.ApiTabletPorta.Value
                : PortaPredefinita;

            if (!attivo)
            {
                FermaInterno();
                UltimoErrore = null;
                return;
            }

            if (InAscolto && porta == Porta) return;

            FermaInterno();

            // La chiave del QR deve esistere prima che arrivi il primo tablet.
            await _databaseService.GetOCreaChiaveDispositivoAsync();

            Avvia(porta);
        }
        catch (Exception ex)
        {
            UltimoErrore = $"Impossibile avviare il servizio: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[ServerDispositivo] {ex}");
        }
        finally
        {
            _bloccoAvvio.Release();
        }
    }

    public void Ferma() => FermaInterno();

    private void Avvia(int porta)
    {
        Porta = porta;

        try
        {
            var listener = new TcpListener(IPAddress.Any, porta);
            listener.Start();

            _listener = listener;
            _annullamento = new CancellationTokenSource();
            UltimoErrore = null;

            var token = _annullamento.Token;
            _ = Task.Run(() => CicloAscoltoAsync(listener, token));

            System.Diagnostics.Debug.WriteLine($"[ServerDispositivo] In ascolto sulla porta {porta}");
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
        {
            UltimoErrore = $"La porta {porta} è già usata da un altro programma.";
        }
        catch (Exception ex)
        {
            UltimoErrore = $"Impossibile avviare il servizio: {ex.Message}";
        }
    }

    private void FermaInterno()
    {
        try { _annullamento?.Cancel(); } catch { }
        _annullamento?.Dispose();
        _annullamento = null;

        try { _listener?.Stop(); } catch { }
        _listener = null;
    }

    // ------------------------------------------------------------------
    // LINK E CHIAVE PER IL QR CODE
    // ------------------------------------------------------------------

    public async Task<string?> GetLinkRegistrazioneAsync()
    {
        if (!InAscolto) return null;

        string? indirizzo = TrovaIndirizzoLocale();
        if (indirizzo == null) return null;

        string chiave = await _databaseService.GetOCreaChiaveDispositivoAsync();

        // Con il modello dispositivo il tablet chiede anche le domande extra.
        string percorso = File.Exists(PrivacyDocumentService.PercorsoModelloDispositivo)
            ? PercorsoConPrivacy
            : PercorsoSenzaPrivacy;

        return $"http://{indirizzo}:{Porta}/{percorso}?k={Uri.EscapeDataString(chiave)}";
    }

    public async Task<string?> RigeneraChiaveAsync()
    {
        await _databaseService.RigeneraChiaveDispositivoAsync();
        return await GetLinkRegistrazioneAsync();
    }

    /// <summary>
    /// Indirizzo del PC nella rete della scuola (es. 192.168.1.23).
    /// Si scartano le schede di rete senza gateway, che di solito sono quelle
    /// virtuali (VirtualBox, Hyper-V, VPN), e si preferiscono gli indirizzi privati.
    /// </summary>
    private static string? TrovaIndirizzoLocale()
    {
        try
        {
            var candidati = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up
                            && n.NetworkInterfaceType != NetworkInterfaceType.Loopback
                            && n.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                .Select(n => n.GetIPProperties())
                .Where(p => p.GatewayAddresses.Any(g =>
                    g.Address.AddressFamily == AddressFamily.InterNetwork
                    && !g.Address.Equals(IPAddress.Any)))
                .SelectMany(p => p.UnicastAddresses)
                .Select(u => u.Address)
                .Where(a => a.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a))
                .ToList();

            var scelto = candidati.FirstOrDefault(IndirizzoPrivato) ?? candidati.FirstOrDefault();
            return scelto?.ToString();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ServerDispositivo] Indirizzo locale non trovato: {ex.Message}");
            return null;
        }
    }

    private static bool IndirizzoPrivato(IPAddress indirizzo)
    {
        byte[] b = indirizzo.GetAddressBytes();
        return b[0] == 10
               || (b[0] == 192 && b[1] == 168)
               || (b[0] == 172 && b[1] >= 16 && b[1] <= 31);
    }

    // ------------------------------------------------------------------
    // CONNESSIONI
    // ------------------------------------------------------------------

    private async Task CicloAscoltoAsync(TcpListener listener, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await listener.AcceptTcpClientAsync(token);
            }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException) { break; }
            catch (SocketException)
            {
                if (token.IsCancellationRequested) break;
                continue;
            }

            _ = Task.Run(() => GestisciConnessioneAsync(client, token));
        }
    }

    private async Task GestisciConnessioneAsync(TcpClient client, CancellationToken tokenServer)
    {
        using (client)
        using (var tempoScaduto = CancellationTokenSource.CreateLinkedTokenSource(tokenServer))
        {
            tempoScaduto.CancelAfter(TempoMassimoRichiesta);
            string? ip = (client.Client.RemoteEndPoint as IPEndPoint)?.Address.ToString();

            try
            {
                var stream = client.GetStream();
                var richiesta = await LeggiRichiestaAsync(stream, tempoScaduto.Token);

                var risposta = richiesta == null
                    ? Risposta.Html(400, PaginaRegistrazioneDispositivo.Messaggio(
                        "Richiesta non valida", "Riprova scansionando il QR code della segreteria."))
                    : await RispondiAsync(richiesta, ip);

                await ScriviRispostaAsync(stream, risposta, tempoScaduto.Token);
            }
            catch (Exception ex)
            {
                // Tablet che chiude la pagina a metà, rete che cade, ecc.:
                // non deve mai arrivare fino all'app.
                System.Diagnostics.Debug.WriteLine($"[ServerDispositivo] Connessione da {ip}: {ex.Message}");
            }
        }
    }

    private async Task<Risposta> RispondiAsync(Richiesta richiesta, string? ip)
    {
        switch (richiesta.Metodo, richiesta.Percorso)
        {
            case ("GET", "/registrazioneprivacy"):
                return await PaginaAsync(richiesta, ip, conDomandeExtra: true);

            case ("GET", "/registrazione"):
                return await PaginaAsync(richiesta, ip, conDomandeExtra: false);

            case ("POST", "/api/registrazione"):
                return await RegistraAsync(richiesta, ip);

            case ("GET", "/favicon.ico"):
                return new Risposta(204, "text/plain", string.Empty);

            default:
                return Risposta.Html(404, PaginaRegistrazioneDispositivo.Messaggio(
                    "Pagina non trovata", "Scansiona di nuovo il QR code che trovi in segreteria."));
        }
    }

    // ------------------------------------------------------------------
    // PAGINA E REGISTRAZIONE
    // ------------------------------------------------------------------

    private async Task<Risposta> PaginaAsync(Richiesta richiesta, string? ip, bool conDomandeExtra)
    {
        richiesta.Query.TryGetValue("k", out var chiave);

        if (!await _databaseService.VerificaChiaveDispositivoAsync(chiave, ip))
        {
            return Risposta.Html(403, PaginaRegistrazioneDispositivo.Messaggio(
                "Link non valido",
                "Questo link non è più attivo. Chiedi in segreteria il QR code aggiornato."));
        }

        var impostazioni = await _impostazioniService.GetImpostazioniAsync();

        var campiExtra = conDomandeExtra
            ? PrivacyDocumentService.LeggiCampiExtraPerDispositivo()
            : new List<CampoExtraDTO>();

        return Risposta.Html(200, PaginaRegistrazioneDispositivo.Modulo(impostazioni.NomeScuola, campiExtra));
    }

    private async Task<Risposta> RegistraAsync(Richiesta richiesta, string? ip)
    {
        RegistrazioneDispositivoDTO? dati;
        try
        {
            dati = JsonSerializer.Deserialize<RegistrazioneDispositivoDTO>(richiesta.Corpo, OpzioniJson);
        }
        catch (JsonException)
        {
            dati = null;
        }

        if (dati == null)
            return Risposta.Json(400, false, "Dati non leggibili. Ricarica la pagina e riprova.");

        if (!await _databaseService.VerificaChiaveDispositivoAsync(dati.Chiave, ip))
            return Risposta.Json(403, false, "Questo link non è più attivo. Chiedi in segreteria il QR code aggiornato.");

        string? errore = Valida(dati, out var allievo);
        if (errore != null || allievo == null)
            return Risposta.Json(400, false, errore ?? "Dati non validi.");

        await _bloccoRegistrazione.WaitAsync();
        try
        {
            if (await _databaseService.EsisteCodiceFiscaleAsync(allievo.CodiceFiscale!))
            {
                return Risposta.Json(409, false,
                    "Questo codice fiscale è già registrato. Contatta l'amministrazione.");
            }

            if (!await _databaseService.SalvaAllievoAsync(allievo))
                return Risposta.Json(500, false, "Registrazione non riuscita. Rivolgiti alla segreteria.");
        }
        finally
        {
            _bloccoRegistrazione.Release();
        }

        await SalvaRisposteExtraAsync(allievo.Id, dati.Extra);

        // Il popup sul PC aspetta un clic della segreteria: non si fa aspettare
        // anche il tablet, che riceve subito la conferma.
        int allievoId = allievo.Id;
        _ = Task.Run(() => _avvisiDispositivoService.SegnalaNuovaRegistrazioneAsync(allievoId));

        return Risposta.Json(200, true, "Registrazione completata.");
    }

    /// <summary>
    /// Controlla i dati e costruisce l'allievo. Restituisce il messaggio d'errore
    /// da mostrare sul tablet, oppure null se è tutto a posto.
    /// </summary>
    private static string? Valida(RegistrazioneDispositivoDTO dati, out Allievi? allievo)
    {
        allievo = null;

        string nome = Pulisci(dati.Nome, 60) ?? string.Empty;
        string cognome = Pulisci(dati.Cognome, 60) ?? string.Empty;
        string codiceFiscale = (dati.CodiceFiscale ?? string.Empty).Trim().ToUpperInvariant();

        if (nome.Length == 0) return "Scrivi il nome.";
        if (cognome.Length == 0) return "Scrivi il cognome.";
        if (!CodiceFiscaleValido(codiceFiscale))
            return "Il codice fiscale non è valido: controlla di averlo scritto bene.";

        string? dataNascita = null;
        if (!string.IsNullOrWhiteSpace(dati.DataNascita))
        {
            if (!DateTime.TryParseExact(dati.DataNascita.Trim(), "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var data)
                || data.Year < 1900 || data > DateTime.Today)
            {
                return "La data di nascita non è valida.";
            }

            // Stesso formato usato dalla scheda allievo.
            dataNascita = data.ToString("yyyy-MM-dd");
        }

        string? email = Pulisci(dati.Email, 120);
        if (email != null && !System.Net.Mail.MailAddress.TryCreate(email, out _))
            return "L'indirizzo email non è valido.";

        string? sesso = dati.Sesso?.Trim() switch
        {
            "M" => "M",
            "F" => "F",
            "Altro" => "Altro",
            _ => null
        };

        allievo = new Allievi
        {
            Nome = nome,
            Cognome = cognome,
            CodiceFiscale = codiceFiscale,
            DataNascita = dataNascita,
            Sesso = sesso,
            Indirizzo = Pulisci(dati.Indirizzo, 120),
            NCivico = Pulisci(dati.Civico, 10),
            Cap = Pulisci(dati.Cap, 10),
            Citta = Pulisci(dati.Citta, 80),
            Provincia = Pulisci(dati.Provincia, 5)?.ToUpperInvariant(),
            Telefono = Pulisci(dati.Telefono, 30),
            Cellulare = Pulisci(dati.Cellulare, 30),
            Email = email,

            // Senza abbonamento: lo fa la segreteria. Il triangolo giallo in
            // Gestione Allievi lo ricorda finché non viene fatto.
            DaAbbonare = 1,
            Origine = "Tablet",
            DataRegistrazione = DateTime.Now
        };

        return null;
    }

    /// <summary>
    /// Salva solo le risposte a domande che esistono davvero nel modello
    /// dispositivo, e per quelle a scelta solo le opzioni previste.
    /// </summary>
    private async Task SalvaRisposteExtraAsync(int allievoId, Dictionary<string, string>? risposte)
    {
        if (risposte == null || risposte.Count == 0) return;

        foreach (var campo in PrivacyDocumentService.LeggiCampiExtraPerDispositivo())
        {
            if (!risposte.TryGetValue(campo.Chiave, out var valore)) continue;

            valore = Pulisci(valore, 200);
            if (valore == null) continue;

            if (campo.IsScelta)
            {
                var opzione = campo.Opzioni.FirstOrDefault(o =>
                    string.Equals(o, valore, StringComparison.OrdinalIgnoreCase));
                if (opzione == null) continue;
                valore = opzione;
            }

            try
            {
                await _databaseService.SalvaCampoExtraAsync(allievoId, campo.Chiave, campo.Etichetta, valore, "Tablet");
            }
            catch (Exception ex)
            {
                // L'allievo è già salvato: una risposta persa non deve annullare tutto.
                System.Diagnostics.Debug.WriteLine($"[ServerDispositivo] Campo {campo.Chiave} non salvato: {ex.Message}");
            }
        }
    }

    private static string? Pulisci(string? valore, int lunghezzaMassima)
    {
        if (string.IsNullOrWhiteSpace(valore)) return null;
        string pulito = valore.Trim();
        return pulito.Length > lunghezzaMassima ? pulito[..lunghezzaMassima] : pulito;
    }

    // ------------------------------------------------------------------
    // CODICE FISCALE
    // ------------------------------------------------------------------

    // Le cifre possono essere sostituite da lettere in caso di omocodia.
    private static readonly Regex FormatoCodiceFiscale = new(
        "^[A-Z]{6}[0-9LMNPQRSTUV]{2}[A-Z][0-9LMNPQRSTUV]{2}[A-Z][0-9LMNPQRSTUV]{3}[A-Z]$",
        RegexOptions.Compiled);

    // Valori dei caratteri in posizione dispari (1a, 3a, ...) per il carattere di
    // controllo. Stessa tabella per 0-9 e A-J.
    private static readonly int[] ValoriDispari =
    {
        1, 0, 5, 7, 9, 13, 15, 17, 19, 21, 2, 4, 18, 20, 11, 3, 6, 8, 12, 14, 16, 10, 22, 25, 24, 23
    };

    /// <summary>
    /// Formato e carattere di controllo (l'ultima lettera). Intercetta quasi
    /// tutti gli errori di battitura sul tablet.
    /// </summary>
    public static bool CodiceFiscaleValido(string codiceFiscale)
    {
        if (!FormatoCodiceFiscale.IsMatch(codiceFiscale)) return false;

        int somma = 0;
        for (int i = 0; i < 15; i++)
        {
            char c = codiceFiscale[i];
            int indice = char.IsDigit(c) ? c - '0' : c - 'A';
            somma += i % 2 == 0 ? ValoriDispari[indice] : indice;
        }

        return codiceFiscale[15] == (char)('A' + somma % 26);
    }

    // ------------------------------------------------------------------
    // HTTP MINIMO
    // ------------------------------------------------------------------

    private sealed record Richiesta(string Metodo, string Percorso, Dictionary<string, string> Query, string Corpo);

    private sealed record Risposta(int Codice, string TipoContenuto, string Corpo)
    {
        public static Risposta Html(int codice, string html) =>
            new(codice, "text/html; charset=utf-8", html);

        public static Risposta Json(int codice, bool ok, string messaggio) =>
            new(codice, "application/json; charset=utf-8", JsonSerializer.Serialize(new { ok, messaggio }));
    }

    private static async Task<Richiesta?> LeggiRichiestaAsync(NetworkStream stream, CancellationToken token)
    {
        var buffer = new byte[8192];
        using var ricevuto = new MemoryStream();
        int fineIntestazioni = -1;

        while (fineIntestazioni < 0)
        {
            int letti = await stream.ReadAsync(buffer, token);
            if (letti == 0) return null;

            ricevuto.Write(buffer, 0, letti);
            if (ricevuto.Length > DimensioneMassimaIntestazioni + DimensioneMassimaCorpo) return null;

            fineIntestazioni = TrovaFineIntestazioni(ricevuto.GetBuffer(), (int)ricevuto.Length);
            if (fineIntestazioni < 0 && ricevuto.Length > DimensioneMassimaIntestazioni) return null;
        }

        byte[] dati = ricevuto.ToArray();
        string[] righe = Encoding.ASCII.GetString(dati, 0, fineIntestazioni).Split("\r\n");

        string[] primaRiga = righe[0].Split(' ');
        if (primaRiga.Length < 2) return null;

        int lunghezzaCorpo = 0;
        foreach (var riga in righe.Skip(1))
        {
            int duePunti = riga.IndexOf(':');
            if (duePunti > 0
                && riga[..duePunti].Trim().Equals("Content-Length", StringComparison.OrdinalIgnoreCase)
                && !int.TryParse(riga[(duePunti + 1)..].Trim(), out lunghezzaCorpo))
            {
                return null;
            }
        }

        if (lunghezzaCorpo < 0 || lunghezzaCorpo > DimensioneMassimaCorpo) return null;

        int inizioCorpo = fineIntestazioni + 4;
        var corpo = new byte[lunghezzaCorpo];
        int giaRicevuti = Math.Min(dati.Length - inizioCorpo, lunghezzaCorpo);
        Array.Copy(dati, inizioCorpo, corpo, 0, giaRicevuti);

        while (giaRicevuti < lunghezzaCorpo)
        {
            int letti = await stream.ReadAsync(corpo.AsMemory(giaRicevuti, lunghezzaCorpo - giaRicevuti), token);
            if (letti == 0) return null;
            giaRicevuti += letti;
        }

        string destinazione = primaRiga[1];
        int punto = destinazione.IndexOf('?');
        string percorso = punto >= 0 ? destinazione[..punto] : destinazione;
        string query = punto >= 0 ? destinazione[(punto + 1)..] : string.Empty;

        return new Richiesta(
            primaRiga[0].ToUpperInvariant(),
            Uri.UnescapeDataString(percorso).TrimEnd('/').ToLowerInvariant(),
            LeggiQuery(query),
            Encoding.UTF8.GetString(corpo));
    }

    private static int TrovaFineIntestazioni(byte[] dati, int lunghezza)
    {
        for (int i = 0; i + 3 < lunghezza; i++)
        {
            if (dati[i] == 13 && dati[i + 1] == 10 && dati[i + 2] == 13 && dati[i + 3] == 10)
                return i;
        }
        return -1;
    }

    private static Dictionary<string, string> LeggiQuery(string query)
    {
        var valori = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var coppia in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            int uguale = coppia.IndexOf('=');
            string chiave = uguale >= 0 ? coppia[..uguale] : coppia;
            string valore = uguale >= 0 ? coppia[(uguale + 1)..] : string.Empty;

            valori[Uri.UnescapeDataString(chiave.Replace('+', ' '))] =
                Uri.UnescapeDataString(valore.Replace('+', ' '));
        }

        return valori;
    }

    private static async Task ScriviRispostaAsync(NetworkStream stream, Risposta risposta, CancellationToken token)
    {
        byte[] corpo = Encoding.UTF8.GetBytes(risposta.Corpo);

        string descrizione = risposta.Codice switch
        {
            200 => "OK",
            204 => "No Content",
            400 => "Bad Request",
            403 => "Forbidden",
            404 => "Not Found",
            409 => "Conflict",
            _ => "Internal Server Error"
        };

        string intestazioni =
            $"HTTP/1.1 {risposta.Codice} {descrizione}\r\n" +
            $"Content-Type: {risposta.TipoContenuto}\r\n" +
            $"Content-Length: {corpo.Length}\r\n" +
            "Cache-Control: no-store\r\n" +
            "X-Content-Type-Options: nosniff\r\n" +
            "Connection: close\r\n\r\n";

        await stream.WriteAsync(Encoding.ASCII.GetBytes(intestazioni), token);
        if (corpo.Length > 0) await stream.WriteAsync(corpo, token);
        await stream.FlushAsync(token);
    }
}
