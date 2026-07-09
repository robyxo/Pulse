namespace Pulse.Utils;

public static class AlertPopup
{
    // =========================================
    //  METODI PRINCIPALI
    // =========================================

    public static async Task Show(string title, string message, string cancel = "OK")
    {
        try
        {
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert(title, message, cancel);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Errore AlertPopup: {ex.Message}");
        }
    }

    public static async Task Show(string title, string message)
    {
        await Show(title, message, "OK");
    }

    public static async Task Show(string title)
    {
        await Show(title, "Funzionalità in sviluppo", "OK");
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
            if (Application.Current?.MainPage != null)
            {
                return await Application.Current.MainPage.DisplayAlert(title, message, confirmText, cancelText);
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    // =========================================
    //  POPUP ERRORE
    // =========================================

    public static async Task ShowError(string message)
    {
        await Show("Errore", message, "OK");
    }

    // =========================================
    //  POPUP ATTENZIONE
    // =========================================

    public static async Task ShowWarning(string message)
    {
        await Show("Attenzione", message, "OK");
    }

    // =========================================
    //  POPUP ELIMINAZIONE
    // =========================================

    public static async Task<bool> ShowDeleteConfirmation(string itemName = "")
    {
        string message = string.IsNullOrEmpty(itemName)
            ? "Sei sicuro di voler procedere con l'eliminazione?"
            : $"Sei sicuro di voler eliminare {itemName}?";

        return await ShowConfirmation(
            "Conferma Eliminazione",
            message,
            "Elimina",
            "Annulla");
    }
}