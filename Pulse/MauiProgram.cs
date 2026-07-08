using CommunityToolkit.Maui;
using Microsoft.Extensions.Configuration;
using Pulse.Services;
using System.Reflection;


namespace Pulse
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            // 1. Caricamento del file di configurazione (appsettings.json) - COMMENTATO PER ORA
            // var assembly = Assembly.GetExecutingAssembly();
            // using var stream = assembly.GetManifestResourceStream("Pulse.appsettings.json");
            //
            // if (stream == null) throw new InvalidOperationException("Il file appsettings.json non è stato trovato come risorsa incorporata.");
            // var config = new ConfigurationBuilder().AddJsonStream(stream).Build();
            //
            // builder.Configuration.AddConfiguration(config);

            // 2. Configurazione Base dell'App e dei Toolkit
            builder.UseMauiApp<App>().UseMauiCommunityToolkit().ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("MaterialSymbols.ttf", "MaterialSymbols");
            });

            /*
            // 3. Registrazione DbContext (MySQL)
            var servicetecDbConnection = builder.Configuration.GetConnectionString("MySqlConnectionServicetec");
            var servicetecDbConnectionTest = builder.Configuration.GetConnectionString("MySqlConnectionServicetecTest");

            if (string.IsNullOrEmpty(servicetecDbConnection) || string.IsNullOrEmpty(servicetecDbConnectionTest))
                throw new InvalidOperationException("Le stringhe di connessione sono mancanti nel file appsettings.json.");

            // Decommenta quando avrai i DbContext pronti
            /*
            builder.Services.AddDbContext<ServicetecContext>(options =>
                options.UseMySql(servicetecDbConnection, ServerVersion.AutoDetect(servicetecDbConnection)));

            builder.Services.AddDbContext<ServicetecContextTest>(options =>
                options.UseMySql(servicetecDbConnectionTest, ServerVersion.AutoDetect(servicetecDbConnectionTest)));
            */

            // 4. Registrazione ViewModels
            builder.Services.AddTransient<MainViewModel>();

            // 5. Registrazione Views
            builder.Services.AddTransient<MainPage>();

            // 6. Registrazione Servizi
            builder.Services.AddSingleton<INavigationService, NavigationService>();

            // 7. Configurazione Logging
            ConfigureWindowsSpecific(builder);

#if DEBUG
            builder.Logging.AddDebug();
            builder.Logging.SetMinimumLevel(LogLevel.Debug);
#endif
            return builder.Build();
        }
        private static void ConfigureWindowsSpecific(MauiAppBuilder builder)
        {
#if WINDOWS
            // Aumenta timeout per operazioni lunghe su Windows
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                Debug.WriteLine($"[Windows] Unhandled Exception: {args.ExceptionObject}");
            };

#endif
        }
    }
}