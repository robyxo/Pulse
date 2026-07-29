using CommunityToolkit.Maui;
using Microsoft.EntityFrameworkCore;
using Pulse.Models;
using Pulse.Services;
using Pulse.ViewModels;
using Pulse.Views;
using Pulse.Views.Popups;

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

        // ⚠️ FLAG DI EMERGENZA:
        // Imposta a 'true' se modifichi lo schema/tabelle del DB e vuoi ricrearlo da zero in Debug.
        // Lascia a 'false' durante il lavoro normale per non perdere i dati salvati.
        bool resetDatabaseDiEmergenza = false;

#if DEBUG
        if (resetDatabaseDiEmergenza && File.Exists(dbPath))
        {
            try { File.Delete(dbPath); } catch { }
        }
#endif

        builder.Services.AddDbContext<PulseContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        // 2. Registrazione ViewModels
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<CalendarioViewModel>();
        builder.Services.AddTransient<CorsiViewModel>();
        builder.Services.AddTransient<GestioneCorsoViewModel>();
        builder.Services.AddTransient<InsegnantiViewModel>();
        builder.Services.AddTransient<GestioneInsegnanteViewModel>();

        // 3. Registrazione Views (Pagine)
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<CalendarioPage>();
        builder.Services.AddTransient<AllieviCorsoPage>();
        builder.Services.AddTransient<StatistichePage>();
        builder.Services.AddTransient<ImpostazioniPage>();
        builder.Services.AddTransient<pagamentiPage>();
        builder.Services.AddTransient<CorsiPage>();
        builder.Services.AddTransient<GestioneCorsoPage>();
        builder.Services.AddTransient<InsegnantiPage>();
        builder.Services.AddTransient<GestioneInsegnantePage>();

        // 4. Registrazione Servizi
        builder.Services.AddSingleton<INavigationService, NavigationService>();
        builder.Services.AddSingleton<IDatabaseService, DatabaseService>();

        ConfigureWindowsSpecific(builder);

#if DEBUG
        builder.Logging.AddDebug();
        builder.Logging.SetMinimumLevel(LogLevel.Debug);
#endif

        var app = builder.Build();

        // 5. Garantisce la creazione del DB se non esiste
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PulseContext>();
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