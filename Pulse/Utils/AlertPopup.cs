namespace Pulse.Utils;

public static class AlertPopup
{
    // Application.Current.MainPage è deprecato in .NET 9: si usa la Shell
    // corrente, con la finestra attiva come riserva nei casi in cui la Shell
    // non sia ancora stata creata (avvio dell'app).
    private static Page? PaginaCorrente =>
        Shell.Current ?? Application.Current?.Windows.FirstOrDefault()?.Page;

    // =========================================
    //  POPUP INFORMATIVO
    // =========================================

    public static async Task Show(string title, string message, string cancel = "OK")
    {
        try
        {
            var pagina = PaginaCorrente;
            if (pagina != null)
            {
                await pagina.DisplayAlert(title, message, cancel);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Errore AlertPopup: {ex.Message}");
        }
    }

    // =========================================
    //  POPUP CONFERMA (SÌ/NO)
    // =========================================

    public static async Task<bool> ShowConfirmation(
        string title,
        string message,
        string confirmText = "Sì",
        string cancelText = "No")
    {
        try
        {
            var pagina = PaginaCorrente;
            if (pagina != null)
            {
                return await pagina.DisplayAlert(title, message, confirmText, cancelText);
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    // =========================================
    //  SCELTA DA ELENCO
    // =========================================

    /// <summary>
    /// Elenco di voci fra cui scegliere. Restituisce il testo della voce scelta,
    /// oppure il testo di annullamento se si chiude senza scegliere.
    /// </summary>
    public static async Task<string> ShowActionSheet(
        string title,
        string? cancel,
        string? destruction,
        params string[] buttons)
    {
        try
        {
            var pagina = PaginaCorrente;
            if (pagina != null)
            {
                return await pagina.DisplayActionSheet(title, cancel, destruction, buttons) ?? cancel ?? string.Empty;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Errore AlertPopup: {ex.Message}");
        }
        return cancel ?? string.Empty;
    }

    // =========================================
    //  RICHIESTA DI UN TESTO
    // =========================================

    /// <summary>
    /// Chiede di scrivere un testo. Restituisce null se si preme Annulla.
    /// </summary>
    public static async Task<string?> ShowPrompt(
        string title,
        string message,
        string accept = "OK",
        string cancel = "Annulla",
        string? placeholder = null,
        int maxLength = -1,
        Keyboard? keyboard = null,
        string initialValue = "")
    {
        try
        {
            var pagina = PaginaCorrente;
            if (pagina != null)
            {
                return await pagina.DisplayPromptAsync(title, message, accept, cancel, placeholder, maxLength, keyboard ?? Keyboard.Default, initialValue);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Errore AlertPopup: {ex.Message}");
        }
        return null;
    }

    // =========================================
    //  POPUP ERRORE / ATTENZIONE
    // =========================================

    public static Task ShowError(string message) =>
        Show("Errore", message, "OK");

    public static Task ShowWarning(string message) =>
        Show("Attenzione", message, "OK");
}