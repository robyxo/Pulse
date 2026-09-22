using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pulse.Models;
using Pulse.Services;
using Microsoft.Maui.ApplicationModel;

namespace Pulse.ViewModels;

public partial class ImpostazioniViewModel : BaseViewModel
{
    private readonly IImpostazioniService _impostazioniService;
    private readonly IEmailService _emailService;
    private readonly IBackupService _backupService;
    private readonly IAggiornamentoService _aggiornamentoService;

    private static readonly Dictionary<string, (string Host, int Porta, bool Ssl)> ProviderNoti =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["gmail.com"] = ("smtp.gmail.com", 587, true),
            ["googlemail.com"] = ("smtp.gmail.com", 587, true),
            ["outlook.com"] = ("smtp.office365.com", 587, true),
            ["outlook.it"] = ("smtp.office365.com", 587, true),
            ["hotmail.com"] = ("smtp.office365.com", 587, true),
            ["hotmail.it"] = ("smtp.office365.com", 587, true),
            ["live.com"] = ("smtp.office365.com", 587, true),
            ["yahoo.com"] = ("smtp.mail.yahoo.com", 587, true),
            ["yahoo.it"] = ("smtp.mail.yahoo.com", 587, true),
            ["libero.it"] = ("smtp.libero.it", 465, true),
            ["virgilio.it"] = ("out.virgilio.it", 465, true),
            ["alice.it"] = ("out.alice.it", 465, true),
            ["tin.it"] = ("out.alice.it", 465, true),
            ["aruba.it"] = ("smtps.aruba.it", 465, true),
            ["pec.aruba.it"] = ("smtps.pec.aruba.it", 465, true),
            ["icloud.com"] = ("smtp.mail.me.com", 587, true),
        };

    private enum ProviderEmail { Gmail, Microsoft, Yahoo, ItalianoGenerico, Sconosciuto }

    private ProviderEmail RilevaProviderCorrente()
    {
        string dominio = string.Empty;
        if (!string.IsNullOrWhiteSpace(EmailSmtpUser) && EmailSmtpUser.Contains('@'))
            dominio = EmailSmtpUser.Split('@').Last().Trim().ToLowerInvariant();

        return dominio switch
        {
            "gmail.com" or "googlemail.com" => ProviderEmail.Gmail,
            "outlook.com" or "outlook.it" or "hotmail.com" or "hotmail.it" or "live.com" => ProviderEmail.Microsoft,
            "yahoo.com" or "yahoo.it" => ProviderEmail.Yahoo,
            "libero.it" or "virgilio.it" or "alice.it" or "tin.it" or "aruba.it" or "pec.aruba.it" => ProviderEmail.ItalianoGenerico,
            _ => ProviderEmail.Sconosciuto
        };
    }

    [RelayCommand]
    public async Task MostraGuidaEmailAsync()
    {
        var provider = RilevaProviderCorrente();
        string titolo;
        string messaggio;
        string? url = null;

        switch (provider)
        {
            case ProviderEmail.Gmail:
                titolo = "Come configurare Gmail";
                messaggio = "Con Gmail non puoi usare la password normale del tuo account: Google la blocca per l'invio automatico.\n\n" +
                            "1. Premi 'Apri Pagina' qui sotto: si aprirà la pagina Google per creare una password per le app (se non hai ancora attivato la Verifica in 2 passaggi, Google te lo chiederà prima).\n" +
                            "2. Scrivi un nome (es. 'Pulse') e premi Crea.\n" +
                            "3. Copia la password di 16 caratteri che appare.\n" +
                            "4. Torna in Pulse, incollala nel campo Password al posto della tua password Gmail normale.\n" +
                            "5. Salva le Impostazioni e riprova il test.";
                url = "https://myaccount.google.com/apppasswords";
                break;

            case ProviderEmail.Microsoft:
                titolo = "Come configurare Outlook / Hotmail / Microsoft 365";
                messaggio = "Se è un account personale (@outlook.it, @hotmail.it, @live.com):\n" +
                            "1. Premi 'Apri Pagina' qui sotto: si aprirà la pagina di sicurezza del tuo account Microsoft.\n" +
                            "2. Attiva la 'Verifica in due passaggi' se non è già attiva.\n" +
                            "3. Cerca 'Opzioni di sicurezza avanzate' > 'Password per le app' e creane una.\n" +
                            "4. Usa quella password di 16 caratteri nel campo Password di Pulse.\n\n" +
                            "Se invece è un indirizzo aziendale/Microsoft 365 (dominio della scuola, gestito da un amministratore):\n" +
                            "Microsoft sta disattivando l'invio SMTP con utente e password per questi account, quindi potrebbe non funzionare comunque. Conviene usare un indirizzo Gmail per l'invio, oppure chiedere all'amministratore IT di riabilitare 'SMTP AUTH' per questa casella nel pannello Microsoft 365.";
                url = "https://account.microsoft.com/security";
                break;

            case ProviderEmail.Yahoo:
                titolo = "Come configurare Yahoo";
                messaggio = "Anche Yahoo richiede una password per le app, non quella normale:\n\n" +
                            "1. Premi 'Apri Pagina' qui sotto: si aprirà la pagina di sicurezza del tuo account Yahoo.\n" +
                            "2. Attiva la verifica in due passaggi se non è già attiva.\n" +
                            "3. Cerca 'Genera password per app', scegli 'Altra app' e scrivi 'Pulse'.\n" +
                            "4. Copia la password generata e incollala nel campo Password di Pulse.\n" +
                            "5. Salva le Impostazioni e riprova il test.";
                url = "https://login.yahoo.com/account/security";
                break;

            case ProviderEmail.ItalianoGenerico:
                titolo = "Come configurare Libero / Aruba / Alice / Tin";
                messaggio = "Con questi provider di solito basta la password normale della webmail (non serve una password per le app).\n\n" +
                            "Se il test fallisce comunque:\n" +
                            "- Controlla di aver scritto correttamente email e password.\n" +
                            "- Prova a cambiare la Porta da 465 a 587 nei campi avanzati (premi '+').\n" +
                            "- Verifica sul sito del tuo gestore che l'accesso 'da programmi esterni' (SMTP) sia abilitato sulla tua casella.";
                break;

            default:
                titolo = "Come configurare la tua email";
                messaggio = "Scrivi prima l'indirizzo email della scuola nel campo Email: Pulse riconoscerà automaticamente i principali provider (Gmail, Outlook, Yahoo, Libero, Aruba...) e mostrerà qui la guida specifica.\n\n" +
                            "In generale, se l'invio fallisce con un errore di autenticazione, quasi sempre serve generare una 'password per le app' dalle impostazioni di sicurezza del tuo account email, invece della password normale.";
                break;
        }

        if (url != null)
        {
            bool apriPagina = await Shell.Current.DisplayAlert(titolo, messaggio, "Apri Pagina", "Chiudi");

            if (apriPagina)
            {
                try
                {
                    await Launcher.Default.OpenAsync(new Uri(url));
                }
                catch (Exception ex)
                {
                    await Shell.Current.DisplayAlert("Errore", $"Impossibile aprire il browser: {ex.Message}", "OK");
                }
            }
        }
        else
        {
            await Shell.Current.DisplayAlert(titolo, messaggio, "OK");
        }
    }

    [ObservableProperty]
    private Impostazioni _impostazioni = new();

    // --- INTESTAZIONE SCUOLA + LOGO ---
    [ObservableProperty]
    private string? _nomeScuola;

    [ObservableProperty]
    private string? _indirizzoScuola;

    [ObservableProperty]
    private string? _partitaIva;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LogoPreview))]
    [NotifyPropertyChangedFor(nameof(LogoPresente))]
    [NotifyPropertyChangedFor(nameof(LogoAssente))]
    private string? _logoPath;

    public ImageSource? LogoPreview =>
        string.IsNullOrWhiteSpace(LogoPath) ? null : ImageSource.FromFile(LogoPath);

    public bool LogoPresente => !string.IsNullOrWhiteSpace(LogoPath);
    public bool LogoAssente => !LogoPresente;

    // --- VISTA CALENDARIO ---
    [ObservableProperty]
    private bool _orarioScaglionatoAttivo;

    // --- RICEVUTE ---
    [ObservableProperty]
    private bool _stampaRicevutaCortesiaAttiva;

    // --- FORMATO RICEVUTA ---
    [ObservableProperty]
    private int _ricevutaLarghezzaMm = 90;

    [ObservableProperty]
    private int _ricevutaAltezzaMm = 90;

    [ObservableProperty]
    private int _ricevutaMarginTopMm = 10;

    [ObservableProperty]
    private int _ricevutaMarginRightMm = 3;

    [ObservableProperty]
    private int _ricevutaMarginBottomMm = 3;

    [ObservableProperty]
    private int _ricevutaMarginLeftMm = 5;

    // --- STAMPA DIRETTA E ARCHIVIO PDF ---
    [ObservableProperty]
    private bool _stampaSilenziosaAttiva;

    [ObservableProperty]
    private bool _archiviaRicevutePdfAttiva;

    [ObservableProperty]
    private string? _cartellaRicevutePdf;

    // --- DOCUMENTO PRIVACY ---
    [ObservableProperty]
    private bool _stampaDocumentoPrivacyAttiva;

    public bool ModelliPrivacyDisponibili => PrivacyDocumentService.AlmenoUnModelloDisponibile;

    // --- COLORI CORSI ---
    [ObservableProperty]
    private int _numeroColoriCorsi = 6;

    public List<int> OpzioniNumeroColori { get; } = Enumerable.Range(1, 20).ToList();

    // --- EMAIL ---
    [ObservableProperty]
    private string? _emailSmtpUser;

    [ObservableProperty]
    private string? _emailSmtpPassword;

    [ObservableProperty]
    private string? _emailMittenteNome;

    [ObservableProperty]
    private string? _emailSmtpHost;

    [ObservableProperty]
    private int _emailSmtpPort = 587;

    [ObservableProperty]
    private bool _emailUseSslAttiva = true;

    [ObservableProperty]
    private bool _mostraCampiAvanzatiEmail;

    [ObservableProperty]
    private string? _emailDestinatarioTest;

    // --- INFO APP ---
    public string VersioneApp => $"Pulse v{AppInfo.Current.VersionString} ({AppInfo.Current.BuildString})";

    // --- SUPPORTO ---
    [ObservableProperty]
    private string? _supportoTitolo;

    [ObservableProperty]
    private string? _supportoDescrizione;

    private const string EmailSupporto = "supportorxo@gmail.com";

    // --- MAESTRO ---

    [ObservableProperty]
    private bool _funzioneMaestroAvanzataAttiva;

    public ImpostazioniViewModel(IImpostazioniService impostazioniService, IEmailService emailService, IBackupService backupService, IAggiornamentoService aggiornamentoService)
    {
        _impostazioniService = impostazioniService;
        _emailService = emailService;
        _backupService = backupService;
        _aggiornamentoService = aggiornamentoService;
        Title = "Impostazioni";
    }

    partial void OnEmailSmtpUserChanged(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.Contains('@'))
            return;

        string dominio = value.Split('@').Last().Trim().ToLowerInvariant();

        if (ProviderNoti.TryGetValue(dominio, out var config))
        {
            EmailSmtpHost = config.Host;
            EmailSmtpPort = config.Porta;
            EmailUseSslAttiva = config.Ssl;
            MostraCampiAvanzatiEmail = false;
        }
        else
        {
            // Provider non riconosciuto: mostriamo i campi avanzati per l'inserimento manuale
            MostraCampiAvanzatiEmail = true;
        }
    }

    [RelayCommand]
    public void ToggleCampiAvanzatiEmail()
    {
        MostraCampiAvanzatiEmail = !MostraCampiAvanzatiEmail;
    }

    [RelayCommand]
    public async Task TestEmailAsync()
    {
        if (string.IsNullOrWhiteSpace(EmailSmtpUser))
        {
            await Shell.Current.DisplayAlert("Attenzione", "Inserisci prima l'email della scuola.", "OK");
            return;
        }

        string destinatario = string.IsNullOrWhiteSpace(EmailDestinatarioTest)
            ? EmailSmtpUser
            : EmailDestinatarioTest.Trim();

        bool conferma = await Shell.Current.DisplayAlert(
            "Test Email",
            $"Verrà inviata una email di prova a:\n{destinatario}\n\nConfermi?",
            "Sì, Invia",
            "Annulla");

        if (!conferma) return;

        await EseguiConCaricamento(async () =>
        {
            await SalvaImpostazioniInternoAsync();

            var (successo, errore) = await _emailService.InviaEmailTestAsync(destinatario);

            if (successo)
                await Shell.Current.DisplayAlert("Fatto", $"Email di prova inviata correttamente a {destinatario}.", "OK");
            else
                await Shell.Current.DisplayAlert("Errore Invio", $"Invio non riuscito:\n{errore}", "OK");
        });
    }

    // --- AGGIORNAMENTI ---
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostraStatoAggiornamento))]
    private string _statoAggiornamento = string.Empty;

    public bool MostraStatoAggiornamento => !string.IsNullOrWhiteSpace(StatoAggiornamento);

    // --- BACKUP E RIPRISTINO ---
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HaBackupPrecedente))]
    private string? _ultimoBackupData;

    public bool HaBackupPrecedente => !string.IsNullOrWhiteSpace(UltimoBackupData);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HaRipristinoPrecedente))]
    private string? _ultimoRipristinoData;

    public bool HaRipristinoPrecedente => !string.IsNullOrWhiteSpace(UltimoRipristinoData);

    public async Task CaricaImpostazioniAsync()
    {
        await EseguiConCaricamento(CaricaImpostazioniInternoAsync);
    }

    private async Task CaricaImpostazioniInternoAsync()
    {
        Impostazioni = await _impostazioniService.GetImpostazioniAsync();

        NomeScuola = Impostazioni.NomeScuola;
        IndirizzoScuola = Impostazioni.IndirizzoScuola;
        PartitaIva = Impostazioni.PartitaIva;
        LogoPath = Impostazioni.LogoPath;

        OrarioScaglionatoAttivo = Impostazioni.OrarioScaglionato == 1;
        StampaRicevutaCortesiaAttiva = Impostazioni.StampaRicevutaCortesia == 1;
        StampaDocumentoPrivacyAttiva = Impostazioni.StampaDocumentoPrivacy == 1;
        NumeroColoriCorsi = Impostazioni.NumeroColoriCorsi ?? 6;

        EmailSmtpUser = Impostazioni.EmailSmtpUser;
        EmailSmtpPassword = Impostazioni.EmailSmtpPassword;
        EmailMittenteNome = Impostazioni.EmailMittenteNome;
        EmailSmtpHost = Impostazioni.EmailSmtpHost;
        EmailSmtpPort = Impostazioni.EmailSmtpPort ?? 587;
        EmailUseSslAttiva = Impostazioni.EmailUseSsl != 0;

        UltimoBackupData = Impostazioni.UltimoBackupData;
        UltimoRipristinoData = Impostazioni.UltimoRipristinoData;

        RicevutaLarghezzaMm = Impostazioni.RicevutaLarghezzaMm ?? 90;
        RicevutaAltezzaMm = Impostazioni.RicevutaAltezzaMm ?? 90;
        RicevutaMarginTopMm = Impostazioni.RicevutaMarginTopMm ?? 10;
        RicevutaMarginRightMm = Impostazioni.RicevutaMarginRightMm ?? 3;
        RicevutaMarginBottomMm = Impostazioni.RicevutaMarginBottomMm ?? 3;
        RicevutaMarginLeftMm = Impostazioni.RicevutaMarginLeftMm ?? 5;

        StampaSilenziosaAttiva = Impostazioni.StampaSilenziosa == 1;
        ArchiviaRicevutePdfAttiva = Impostazioni.ArchiviaRicevutePdf == 1;
        CartellaRicevutePdf = Impostazioni.CartellaRicevutePdf;

        FunzioneMaestroAvanzataAttiva = Impostazioni.FunzioneMaestroAvanzataAttiva == 1;
    }

    [RelayCommand]
    public async Task SceglieCartellaRicevuteAsync()
    {
        try
        {
            var risultato = await FolderPicker.Default.PickAsync(CancellationToken.None);

            if (risultato.IsSuccessful && risultato.Folder is not null)
            {
                CartellaRicevutePdf = risultato.Folder.Path;
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert(
                "Errore",
                $"Impossibile scegliere la cartella: {ex.Message}\n\nPuoi comunque incollare il percorso a mano.",
                "OK");
        }
    }

    [RelayCommand]
    public async Task SceglieLogoAsync()
    {
        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Seleziona il logo della scuola",
                FileTypes = FilePickerFileType.Images
            });

            if (file == null) return;

            string estensione = Path.GetExtension(file.FileName);
            string cartellaLoghi = Path.Combine(FileSystem.AppDataDirectory, "Logo");
            Directory.CreateDirectory(cartellaLoghi);

            foreach (var vecchio in Directory.GetFiles(cartellaLoghi, "logo.*"))
            {
                File.Delete(vecchio);
            }

            string destinazione = Path.Combine(cartellaLoghi, $"logo{estensione}");

            using (var origine = await file.OpenReadAsync())
            using (var copia = File.Create(destinazione))
            {
                await origine.CopyToAsync(copia);
            }

            LogoPath = destinazione;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Errore", $"Impossibile caricare il logo: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    public void RimuoviLogo()
    {
        if (!string.IsNullOrWhiteSpace(LogoPath) && File.Exists(LogoPath))
        {
            File.Delete(LogoPath);
        }

        LogoPath = null;
    }

    private async Task SalvaImpostazioniInternoAsync()
    {
        Impostazioni.NomeScuola = NomeScuola?.Trim();
        Impostazioni.IndirizzoScuola = IndirizzoScuola?.Trim();
        Impostazioni.PartitaIva = PartitaIva?.Trim();
        Impostazioni.LogoPath = LogoPath;

        Impostazioni.OrarioScaglionato = OrarioScaglionatoAttivo ? 1 : 0;
        Impostazioni.StampaRicevutaCortesia = StampaRicevutaCortesiaAttiva ? 1 : 0;
        Impostazioni.StampaDocumentoPrivacy = StampaDocumentoPrivacyAttiva ? 1 : 0;
        Impostazioni.NumeroColoriCorsi = NumeroColoriCorsi;

        Impostazioni.EmailSmtpUser = EmailSmtpUser?.Trim();
        Impostazioni.EmailSmtpPassword = EmailSmtpPassword;
        Impostazioni.EmailMittenteNome = EmailMittenteNome?.Trim();
        Impostazioni.EmailSmtpHost = EmailSmtpHost?.Trim();
        Impostazioni.EmailSmtpPort = EmailSmtpPort;
        Impostazioni.EmailUseSsl = EmailUseSslAttiva ? 1 : 0;

        Impostazioni.RicevutaLarghezzaMm = RicevutaLarghezzaMm;
        Impostazioni.RicevutaAltezzaMm = RicevutaAltezzaMm;
        Impostazioni.RicevutaMarginTopMm = RicevutaMarginTopMm;
        Impostazioni.RicevutaMarginRightMm = RicevutaMarginRightMm;
        Impostazioni.RicevutaMarginBottomMm = RicevutaMarginBottomMm;
        Impostazioni.RicevutaMarginLeftMm = RicevutaMarginLeftMm;

        Impostazioni.StampaSilenziosa = StampaSilenziosaAttiva ? 1 : 0;
        Impostazioni.ArchiviaRicevutePdf = ArchiviaRicevutePdfAttiva ? 1 : 0;
        Impostazioni.CartellaRicevutePdf = CartellaRicevutePdf?.Trim();

        Impostazioni.FunzioneMaestroAvanzataAttiva = FunzioneMaestroAvanzataAttiva ? 1 : 0;

        await _impostazioniService.SalvaImpostazioniAsync(Impostazioni);
    }

    [RelayCommand]
    public async Task SalvaImpostazioniAsync()
    {
        await EseguiConCaricamento(async () =>
        {
            await SalvaImpostazioniInternoAsync();
            await Shell.Current.DisplayAlert("Fatto", "Impostazioni salvate correttamente.", "OK");
        });
    }

    [RelayCommand]
    public async Task EseguiBackupAsync()
    {
        await EseguiConCaricamento(async () =>
        {
            var (successo, percorso, errore) = await _backupService.EseguiBackupAsync();

            if (successo)
            {
                await CaricaImpostazioniInternoAsync();
                await Shell.Current.DisplayAlert("Fatto", $"Backup creato con successo:\n{percorso}", "OK");
            }
            else if (errore != null)
            {
                await Shell.Current.DisplayAlert("Errore Backup", errore, "OK");
            }
        });
    }

    [RelayCommand]
    public async Task RipristinaBackupAsync()
    {
        var tipoFileDb = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
    {
        { DevicePlatform.WinUI, new[] { ".db" } },
        { DevicePlatform.MacCatalyst, new[] { "db" } },
    });

        var file = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Seleziona il file di backup (.db) da ripristinare",
            FileTypes = tipoFileDb
        });

        if (file == null) return;

        bool conferma = await Shell.Current.DisplayAlert(
            "⚠️ Attenzione",
            $"Stai per sovrascrivere TUTTI i dati attuali di Pulse con il contenuto di:\n{file.FileName}\n\nQuesta operazione non si può annullare. Continuare?",
            "Sì, Ripristina",
            "Annulla");

        if (!conferma) return;

        await EseguiConCaricamento(async () =>
        {
            var (successo, errore) = await _backupService.RipristinaBackupAsync(file.FullPath);

            if (successo)
            {
                await Shell.Current.DisplayAlert(
                    "Ripristino completato",
                    "Il database è stato ripristinato.\n\nChiudi completamente Pulse e riaprilo ora, altrimenti l'app continuerà a mostrare i vecchi dati rimasti in memoria.",
                    "OK");
            }
            else
            {
                await Shell.Current.DisplayAlert("Errore Ripristino", errore ?? "Errore sconosciuto.", "OK");
            }
        });
    }

    [RelayCommand]
    public async Task ResetApplicazioneAsync()
    {
        bool primaConferma = await Shell.Current.DisplayAlert(
            "⚠️ Reset Applicazione",
            "Questa operazione cancellerà TUTTI i dati di Pulse: allievi, corsi, lezioni, abbonamenti, iscrizioni, presenze, documenti privacy, chiusure calendario e tutte le impostazioni (logo, email, colori, ecc.).\n\nVuoi continuare?",
            "Sì, Continua",
            "Annulla");

        if (!primaConferma) return;

        bool secondaConferma = await Shell.Current.DisplayAlert(
            "⚠️ Sei sicuro?",
            "Questa azione NON si può annullare. Se ti servono i dati attuali, esci ora e fai prima un Backup dalla sezione qui sopra.\n\nVuoi procedere comunque con la cancellazione totale?",
            "Sì, Procedi",
            "Annulla");

        if (!secondaConferma) return;

        string? testoConferma = await Shell.Current.DisplayPromptAsync(
            "Conferma finale",
            "Per confermare in modo definitivo, scrivi la parola RESET (in maiuscolo) e premi OK.",
            "OK",
            "Annulla",
            placeholder: "RESET");

        if (!string.Equals(testoConferma?.Trim(), "RESET", StringComparison.Ordinal))
        {
            if (testoConferma != null)
            {
                await Shell.Current.DisplayAlert("Annullato", "Reset annullato: la parola scritta non corrisponde.", "OK");
            }
            return;
        }

        await EseguiConCaricamento(async () =>
        {
            var (successo, errore) = await _backupService.ResetApplicazioneAsync();

            if (successo)
            {
                await Shell.Current.DisplayAlert(
                    "Fatto",
                    "Tutti i dati sono stati cancellati. Chiudi e riapri Pulse per ripartire da zero.",
                    "OK");
            }
            else
            {
                await Shell.Current.DisplayAlert("Errore", $"Reset non riuscito:\n{errore}", "OK");
            }
        });
    }

    [RelayCommand]
    public async Task ControllaAggiornamentiAsync()
    {
        await EseguiConCaricamento(async () =>
        {
            StatoAggiornamento = "Controllo in corso...";

            var (ceUnAggiornamento, nuovaVersione, errore) = await _aggiornamentoService.ControllaAggiornamentiAsync();

            if (errore != null)
            {
                StatoAggiornamento = errore;
                await Shell.Current.DisplayAlert("Aggiornamenti", errore, "OK");
                return;
            }

            if (!ceUnAggiornamento)
            {
                StatoAggiornamento = "Pulse è già aggiornato all'ultima versione.";
                await Shell.Current.DisplayAlert("Aggiornamenti", StatoAggiornamento, "OK");
                return;
            }

            bool vuoleAggiornare = await Shell.Current.DisplayAlert(
                "Aggiornamento disponibile",
                $"È disponibile la versione {nuovaVersione}.\n\nVuoi scaricarla e installarla adesso?\nPulse si chiuderà e si riaprirà da solo al termine.",
                "Sì, aggiorna",
                "Più tardi");

            if (!vuoleAggiornare)
            {
                StatoAggiornamento = $"Versione {nuovaVersione} disponibile: puoi installarla quando vuoi.";
                return;
            }

            var progresso = new Progress<int>(p => StatoAggiornamento = $"Scaricamento in corso... {p}%");

            var (successo, erroreDownload) = await _aggiornamentoService.ScaricaEInstallaAsync(progresso);

            if (!successo)
            {
                StatoAggiornamento = "Aggiornamento non riuscito.";
                await Shell.Current.DisplayAlert("Errore", $"Non è stato possibile aggiornare Pulse:\n{erroreDownload}", "OK");
            }
        });
    }

    [RelayCommand]
    public async Task InviaSegnalazioneSupportoAsync()
    {
        if (string.IsNullOrWhiteSpace(SupportoTitolo) || string.IsNullOrWhiteSpace(SupportoDescrizione))
        {
            await Shell.Current.DisplayAlert("Attenzione", "Inserisci sia il titolo che la descrizione del problema.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(EmailSmtpHost) || string.IsNullOrWhiteSpace(EmailSmtpUser))
        {
            await Shell.Current.DisplayAlert(
                "Email non configurata",
                "Per inviare una segnalazione al supporto devi prima configurare l'email della scuola nella sezione 'Email Scuola' qui sopra.",
                "OK");
            return;
        }

        bool conferma = await Shell.Current.DisplayAlert(
            "Invia Segnalazione",
            $"Vuoi inviare questa segnalazione al supporto?\n\nTitolo: {SupportoTitolo}",
            "Sì, Invia",
            "Annulla");

        if (!conferma) return;

        await EseguiConCaricamento(async () =>
        {
            string oggetto = $"[Pulse - Supporto] {SupportoTitolo!.Trim()}";
            string messaggio =
                $"Segnalazione inviata da: {(string.IsNullOrWhiteSpace(NomeScuola) ? "Scuola non configurata" : NomeScuola)}\n" +
                $"Email mittente: {EmailSmtpUser}\n" +
                $"Versione App: {VersioneApp}\n\n" +
                $"Titolo: {SupportoTitolo.Trim()}\n\n" +
                $"Descrizione:\n{SupportoDescrizione!.Trim()}";

            bool esito = await _emailService.InviaEmailAsync(new List<string> { EmailSupporto }, oggetto, messaggio);

            if (esito)
            {
                await Shell.Current.DisplayAlert(
                    "Fatto",
                    "Segnalazione inviata! Le eventuali risposte del supporto arriveranno nella tua casella di posta email, non in questa app.",
                    "OK");
                SupportoTitolo = string.Empty;
                SupportoDescrizione = string.Empty;
            }
            else
            {
                await Shell.Current.DisplayAlert("Errore Invio", "Non è stato possibile inviare la segnalazione. Controlla la configurazione email.", "OK");
            }
        });
    }
}