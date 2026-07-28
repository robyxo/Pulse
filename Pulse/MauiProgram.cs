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

        // 1. Percorso del Database
        var dbFileName = "Pulse.db";
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, dbFileName);

#if DEBUG
        // 💡 FORZIAMO LA CANCELLAZIONE DEL VECCHIO DB DALLA DISCO PER QUESTA VOLTA,
        // COSÌ DA ELIMINARE PER SEMPRE L'ERRORE DELLA COLONNA MANCANTE!
        if (File.Exists(dbPath))
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

        // 3. Registrazione Views (Pagine)
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<CalendarioPage>();
        builder.Services.AddTransient<AllieviCorsoPage>();
        builder.Services.AddTransient<StatistichePage>();
        builder.Services.AddTransient<ImpostazioniPage>();
        builder.Services.AddTransient<pagamentiPage>();
        builder.Services.AddTransient<CorsiPage>();
        builder.Services.AddTransient<GestioneCorsoPage>();

        // 4. Registrazione Servizi
        builder.Services.AddSingleton<INavigationService, NavigationService>();
        builder.Services.AddSingleton<IDatabaseService, DatabaseService>();

        ConfigureWindowsSpecific(builder);

#if DEBUG
        builder.Logging.AddDebug();
        builder.Logging.SetMinimumLevel(LogLevel.Debug);
#endif

        var app = builder.Build();

        // 5. Ricrea il database con TUTTE le nuove colonne
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PulseContext>();
            db.Database.EnsureCreated(); // Ricrea il DB perfetto con CostoAnnuale
        }

        return app;
    }

    private static void ConfigureWindowsSpecific(MauiAppBuilder builder)
    {
#if WINDOWS
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            System.Diagnostics.Debug.WriteLine($"[Windows] Unhandled Exception: {args.ExceptionObject}");
        };
#endif
    }
}