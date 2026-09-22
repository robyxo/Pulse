#if WINDOWS
using Microsoft.Web.WebView2.Core;
#endif

namespace Pulse.Services;

/// <summary>
/// Implementazione di <see cref="IStampaService"/> basata su WebView2.
///
/// Come funziona su Windows:
///   1. l'HTML viene scritto in un file temporaneo nella cache;
///   2. si crea un WebView2 invisibile (1x1 px) agganciato alla finestra dell'app;
///   3. si aspetta il caricamento della pagina;
///   4. si chiama PrintAsync (stampa diretta) oppure PrintToPdfAsync (archiviazione).
///
/// WebView2 e' gia' installato su Windows 10/11 e viene gia' usato da MAUI,
/// quindi non serve nessuna dipendenza aggiuntiva.
/// </summary>
public class StampaService : IStampaService
{
    // WebView2 ragiona in pollici: A4 = 210x297 mm.
    private const double A4LarghezzaPollici = 8.27;
    private const double A4AltezzaPollici = 11.69;

    // Oltre questo tempo si rinuncia: meglio ricadere sull'anteprima
    // che lasciare la segreteria con la rotella che gira.
    private static readonly TimeSpan TimeoutCaricamento = TimeSpan.FromSeconds(15);

#if WINDOWS
    public bool SupportaStampaSilenziosa => true;
#else
    public bool SupportaStampaSilenziosa => false;
#endif

    public async Task<bool> StampaHtmlAsync(string html, string? nomeStampante = null)
    {
#if WINDOWS
        return await EseguiConWebViewAsync(html, async (core, env) =>
        {
            var impostazioni = CreaImpostazioniStampa(env);

            // Nome vuoto = stampante predefinita di Windows.
            if (!string.IsNullOrWhiteSpace(nomeStampante))
            {
                impostazioni.PrinterName = nomeStampante;
            }

            var esito = await core.PrintAsync(impostazioni);
            return esito == CoreWebView2PrintStatus.Succeeded;
        });
#else
        await Task.CompletedTask;
        return false;
#endif
    }

    public async Task<bool> SalvaHtmlComePdfAsync(string html, string percorsoPdf)
    {
#if WINDOWS
        if (string.IsNullOrWhiteSpace(percorsoPdf)) return false;

        var cartella = Path.GetDirectoryName(percorsoPdf);
        if (!string.IsNullOrWhiteSpace(cartella))
        {
            Directory.CreateDirectory(cartella);
        }

        return await EseguiConWebViewAsync(html, async (core, env) =>
        {
            var impostazioni = CreaImpostazioniStampa(env);
            return await core.PrintToPdfAsync(percorsoPdf, impostazioni);
        });
#else
        await Task.CompletedTask;
        return false;
#endif
    }

#if WINDOWS

    /// <summary>
    /// Ciclo di vita completo del WebView2 invisibile: creazione, caricamento,
    /// esecuzione dell'operazione richiesta e pulizia (anche in caso di errore).
    /// Tutto gira sul thread della UI, come richiesto da WebView2.
    /// </summary>
    private static Task<bool> EseguiConWebViewAsync(
        string html,
        Func<CoreWebView2, CoreWebView2Environment, Task<bool>> operazione)
    {
        return MainThread.InvokeOnMainThreadAsync(async () =>
        {
            string fileTemporaneo = Path.Combine(
                FileSystem.CacheDirectory,
                $"stampa_{Guid.NewGuid():N}.html");

            CoreWebView2Controller? controller = null;

            try
            {
                await File.WriteAllTextAsync(fileTemporaneo, html);

                IntPtr finestra = RecuperaHandleFinestra();
                if (finestra == IntPtr.Zero)
                {
                    System.Diagnostics.Debug.WriteLine("[StampaService] Finestra principale non disponibile.");
                    return false;
                }

                // Qui WebView2 arriva dal Windows App SDK (proiezione WinRT):
                // l'ambiente si crea senza parametri e la finestra si passa
                // tramite CoreWebView2ControllerWindowReference, non come IntPtr.
                var ambiente = await CoreWebView2Environment.CreateAsync();

                var riferimentoFinestra = CoreWebView2ControllerWindowReference
                    .CreateFromWindowHandle((ulong)finestra.ToInt64());

                controller = await ambiente.CreateCoreWebView2ControllerAsync(riferimentoFinestra);
                controller.IsVisible = false;
                controller.Bounds = new Windows.Foundation.Rect(0, 0, 1, 1);

                var core = controller.CoreWebView2;

                var caricamento = new TaskCompletionSource<bool>();

                // Evento WinRT: il mittente e' tipizzato, non e' un object.
                void AlCaricamentoCompletato(CoreWebView2 mittente, CoreWebView2NavigationCompletedEventArgs args)
                    => caricamento.TrySetResult(args.IsSuccess);

                core.NavigationCompleted += AlCaricamentoCompletato;
                core.Navigate(new Uri(fileTemporaneo).AbsoluteUri);

                var completata = await Task.WhenAny(caricamento.Task, Task.Delay(TimeoutCaricamento));
                core.NavigationCompleted -= AlCaricamentoCompletato;

                if (completata != caricamento.Task)
                {
                    System.Diagnostics.Debug.WriteLine("[StampaService] Timeout nel caricamento della pagina.");
                    return false;
                }

                if (!caricamento.Task.Result)
                {
                    System.Diagnostics.Debug.WriteLine("[StampaService] Caricamento della pagina fallito.");
                    return false;
                }

                return await operazione(core, ambiente);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StampaService] Errore: {ex}");
                return false;
            }
            finally
            {
                controller?.Close();

                try
                {
                    if (File.Exists(fileTemporaneo)) File.Delete(fileTemporaneo);
                }
                catch
                {
                    // Il file resta in cache: nessun problema, verra' ripulito dal sistema.
                }
            }
        });
    }

    /// <summary>Handle Win32 della finestra principale dell'app.</summary>
    private static IntPtr RecuperaHandleFinestra()
    {
        var finestraMaui = Application.Current?.Windows?.FirstOrDefault();
        if (finestraMaui?.Handler?.PlatformView is not Microsoft.UI.Xaml.Window finestraNativa)
        {
            return IntPtr.Zero;
        }

        return WinRT.Interop.WindowNative.GetWindowHandle(finestraNativa);
    }

    /// <summary>
    /// Foglio A4 verticale a margine zero: la posizione del foglietto 90x90
    /// e' gia' decisa dal CSS della ricevuta, quindi qui non si aggiunge nulla.
    /// </summary>
    private static CoreWebView2PrintSettings CreaImpostazioniStampa(CoreWebView2Environment ambiente)
    {
        var impostazioni = ambiente.CreatePrintSettings();

        impostazioni.Orientation = CoreWebView2PrintOrientation.Portrait;
        impostazioni.PageWidth = A4LarghezzaPollici;
        impostazioni.PageHeight = A4AltezzaPollici;
        impostazioni.MarginTop = 0;
        impostazioni.MarginBottom = 0;
        impostazioni.MarginLeft = 0;
        impostazioni.MarginRight = 0;
        impostazioni.ScaleFactor = 1.0;
        impostazioni.ShouldPrintBackgrounds = true;
        impostazioni.ShouldPrintHeaderAndFooter = false;

        return impostazioni;
    }

#endif
}
