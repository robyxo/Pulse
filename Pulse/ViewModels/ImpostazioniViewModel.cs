using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pulse.Models;
using Pulse.Services;

namespace Pulse.ViewModels;

public partial class ImpostazioniViewModel : BaseViewModel
{
    private readonly IImpostazioniService _impostazioniService;
    private readonly IEmailService _emailService;

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

    // --- DOCUMENTO PRIVACY ---
    [ObservableProperty]
    private bool _stampaDocumentoPrivacyAttiva;

    // --- COLORI CORSI ---
    [ObservableProperty]
    private int _numeroColoriCorsi = 6;

    public List<int> OpzioniNumeroColori { get; } = Enumerable.Range(1, 20).ToList();

    // --- EMAIL SCUOLA ---
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

    public ImpostazioniViewModel(INavigationService navigationService, IImpostazioniService impostazioniService, IEmailService emailService)
        : base(navigationService)
    {
        _impostazioniService = impostazioniService;
        _emailService = emailService;
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

        bool conferma = await Shell.Current.DisplayAlert(
            "Test Email",
            $"Verrà inviata una email di prova a:\n{EmailSmtpUser}\n\nConfermi?",
            "Sì, Invia",
            "Annulla");

        if (!conferma) return;

        await EseguiConCaricamento(async () =>
        {
            await SalvaImpostazioniInternoAsync();

            var (successo, errore) = await _emailService.InviaEmailTestAsync(EmailSmtpUser);

            if (successo)
                await Shell.Current.DisplayAlert("Fatto", $"Email di prova inviata correttamente a {EmailSmtpUser}.", "OK");
            else
                await Shell.Current.DisplayAlert("Errore Invio", $"Invio non riuscito:\n{errore}", "OK");
        });
    }

    [RelayCommand]
    public async Task CaricaImpostazioniAsync()
    {
        await EseguiConCaricamento(async () =>
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
        });
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
}