using CommunityToolkit.Maui;
using Microsoft.EntityFrameworkCore;
using Pulse.Models;
using Pulse.Services;

namespace Pulse;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder.UseMauiApp<App>()
          .UseMauiCommunityToolkit()
          .ConfigureFonts(fonts =>
          {
              fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
              fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
              fonts.AddFont("MaterialSymbols.ttf", "MaterialSymbols");
          });

        // 1. Percorso del Database SQLite in AppData
        var dbFileName = "Pulse.db";
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, dbFileName);

#if DEBUG
        // ⚠️ FLAG DI EMERGENZA:
        // Imposta a 'true' se modifichi lo schema/tabelle del DB e vuoi ricrearlo da zero in Debug.
        // Lascia a 'false' durante il lavoro normale per non perdere i dati salvati.
        bool resetDatabaseDiEmergenza = false;

        if (resetDatabaseDiEmergenza && File.Exists(dbPath))
        {
            try
            {
                File.Delete(dbPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Impossibile eliminare il database: {ex.Message}");
            }
        }
#endif

        builder.Services.AddDbContextFactory<PulseContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));


        // 2. Registrazione ViewModels
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<CalendarioViewModel>();
        builder.Services.AddTransient<CorsiViewModel>();
        builder.Services.AddTransient<GestioneCorsoViewModel>();
        builder.Services.AddTransient<InsegnantiViewModel>();
        builder.Services.AddTransient<GestioneInsegnanteViewModel>();
        builder.Services.AddTransient<AllieviViewModel>();
        builder.Services.AddTransient<GestioneAllievoViewModel>();
        builder.Services.AddTransient<GestioneLezioneViewModel>();
        builder.Services.AddTransient<NotificheViewModel>();
        builder.Services.AddTransient<ImpostazioniViewModel>();
        builder.Services.AddTransient<CalendarioEventiViewModel>();

        // 3. Registrazione Views (Pagine)
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<CalendarioPage>();
        builder.Services.AddTransient<CorsiPage>();
        builder.Services.AddTransient<GestioneCorsoPage>();
        builder.Services.AddTransient<InsegnantiPage>();
        builder.Services.AddTransient<GestioneInsegnantePage>();
        builder.Services.AddTransient<AllieviPage>();
        builder.Services.AddTransient<GestioneAllievoPage>();
        builder.Services.AddTransient<GestioneLezionePage>();
        builder.Services.AddTransient<NotifichePage>();
        builder.Services.AddTransient<ImpostazioniPage>();
        builder.Services.AddTransient<CalendarioEventiPage>();

        // 4. Registrazione Servizi
        builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
        builder.Services.AddSingleton<IEmailService, EmailService>();
        builder.Services.AddSingleton<IImpostazioniService, ImpostazioniService>();
        builder.Services.AddSingleton<RicevutaService>();
        builder.Services.AddSingleton<IBackupService, BackupService>();
        builder.Services.AddSingleton<CompensiMaestriService>();
        builder.Services.AddSingleton<PrivacyDocumentService>();
        builder.Services.AddSingleton<IAggiornamentoService, AggiornamentoService>();

        ConfigureWindowsSpecific(builder);

#if DEBUG
        builder.Logging.AddDebug();
        builder.Logging.SetMinimumLevel(LogLevel.Debug);
#endif

        var app = builder.Build();

        // 5. Garantisce la creazione del DB se non esiste
        using (var db = app.Services.GetRequiredService<IDbContextFactory<PulseContext>>().CreateDbContext())
        {
            db.Database.EnsureCreated();
        }

        return app;
    }

    private static void ConfigureWindowsSpecific(MauiAppBuilder builder)
    {
#if WINDOWS
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {System.Diagnostics.Debug.WriteLine($"[Windows] Unhandled Exception: {args.ExceptionObject}");};
#endif
    }
}