using CommunityToolkit.Maui;
using Microsoft.EntityFrameworkCore;
using Pulse.DataBase; // <-- per DbSeeder
using Pulse.Models;
using Pulse.Services;
using Pulse.Views.Popups;
using System.Reflection;

namespace Pulse
{
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

            // 3. DB SQLite
            var dbFileName = "Pulse.db";
#if DEBUG
            dbFileName = "PulseDemo.db";
#endif
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, dbFileName);

#if DEBUG
            if (File.Exists(dbPath)) File.Delete(dbPath);
#endif

            if (!File.Exists(dbPath))
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "Pulse.DataBase.Pulse.db";
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                    throw new InvalidOperationException($"Risorsa {resourceName} non trovata. Controlla che Pulse.db sia EmbeddedResource nel csproj.");
                using var fileStream = File.Create(dbPath);
                stream.CopyTo(fileStream);
            }

            builder.Services.AddDbContext<PulseContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));

            // 4. ViewModels
            builder.Services.AddTransient<MainViewModel>();
            builder.Services.AddTransient<CalendarioViewModel>();

            // 5. Views
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<CalendarioPage>();
            builder.Services.AddTransient<AllieviCorsoPage>();

            // 6. Servizi
            builder.Services.AddSingleton<INavigationService, NavigationService>();
            builder.Services.AddSingleton<IDatabaseService, DatabaseService>();

            ConfigureWindowsSpecific(builder);

#if DEBUG
            builder.Logging.AddDebug();
            builder.Logging.SetMinimumLevel(LogLevel.Debug);
#endif

            var app = builder.Build();

            // QUESTO MANCAVA: CREA LE TABELLE E POPOLA I DATI DEMO
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<PulseContext>();
                db.Database.EnsureCreated(); // crea le 6 tabelle se non esistono
#if DEBUG
                DbSeeder.SeedAsync(db).GetAwaiter().GetResult(); // popola i dati
#endif
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
}