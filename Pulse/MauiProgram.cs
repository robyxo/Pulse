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
        var dbFolder = FileSystem.AppDataDirectory;

        // Su Windows non pacchettizzato questa cartella può non esistere ancora:
        // senza crearla, SQLite risponde "unable to open database file".
        Directory.CreateDirectory(dbFolder);

        var dbPath = Path.Combine(dbFolder, dbFileName);

        // Se il database non c'è ma restano i file di appoggio di una sessione
        // precedente, SQLite si rifiuta di ricrearlo: "unable to open database file".
        if (!File.Exists(dbPath))
        {
            foreach (var residuo in new[] { dbPath + "-wal", dbPath + "-shm", dbPath + "-journal" })
            {
                try
                {
                    if (File.Exists(residuo)) File.Delete(residuo);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Impossibile eliminare {residuo}: {ex.Message}");
                }
            }
        }

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
        builder.Services.AddTransient<SaleViewModel>();
        builder.Services.AddTransient<GestioneSalaViewModel>();
        builder.Services.AddTransient<StatisticheViewModel>();

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
        builder.Services.AddTransient<SalePage>();
        builder.Services.AddTransient<GestioneSalaPage>();
        builder.Services.AddTransient<StatistichePage>();


        // 4. Registrazione Servizi
        builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
        builder.Services.AddSingleton<IEmailService, EmailService>();
        builder.Services.AddSingleton<IImpostazioniService, ImpostazioniService>();
        builder.Services.AddSingleton<IStampaService, StampaService>();
        builder.Services.AddSingleton<RicevutaService>();
        builder.Services.AddSingleton<IBackupService, BackupService>();
        builder.Services.AddSingleton<CompensiMaestriService>();
        builder.Services.AddSingleton<PrivacyDocumentService>();
        builder.Services.AddSingleton<IAggiornamentoService, AggiornamentoService>();
        builder.Services.AddSingleton<IMigrazioneDbService, MigrazioneDbService>();
        builder.Services.AddSingleton<IAvvisiDispositivoService, AvvisiDispositivoService>();
        builder.Services.AddSingleton<IServerDispositivoService, ServerDispositivoService>();
        builder.Services.AddSingleton<IStatisticheService, StatisticheService>();

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

        // 6. Allinea lo schema sulle installazioni già esistenti (EnsureCreated non lo fa)
        app.Services.GetRequiredService<IMigrazioneDbService>()
            .AggiornaSchemaAsync().GetAwaiter().GetResult();

        // 7. Accende il collegamento con il tablet, se attivo nelle impostazioni
        _ = Task.Run(() => app.Services.GetRequiredService<IServerDispositivoService>().AggiornaDaImpostazioniAsync());

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