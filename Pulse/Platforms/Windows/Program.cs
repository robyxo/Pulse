using Microsoft.UI.Dispatching;
using Velopack;

namespace Pulse.WinUI;

public static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        // DEVE restare la primissima riga eseguita dall'applicazione.
        // Velopack usa questo punto per gestire installazione, aggiornamento e
        // disinstallazione: in quei casi fa il suo lavoro e chiude il processo,
        // senza mai arrivare ad aprire una finestra.
        VelopackApp.Build().Run();

        WinRT.ComWrappersSupport.InitializeComWrappers();

        Microsoft.UI.Xaml.Application.Start(p =>
        {
            var context = new DispatcherQueueSynchronizationContext(
                DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            _ = new App();
        });
    }
}