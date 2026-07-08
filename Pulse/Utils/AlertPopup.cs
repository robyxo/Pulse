using Pulse.Component;
using Pulse.Resources.Strings;
using CommunityToolkit.Maui.Views;

namespace Pulse.Utils;
public static class AlertPopup
{
    private static async Task ShowCustomPopup(string title, string message, string cancel)
    {
        var popup = new CustomPopup(title, message, cancel);
        if (Shell.Current?.CurrentPage != null)
        {
            await Shell.Current.CurrentPage.ShowPopupAsync(popup);
        }
    }

    // =========================================
    //     METODI PUBBLICI PER L'USO ESTERNO
    // =========================================

    public static async Task Show(string title, string message, string cancel) => await ShowCustomPopup(title, message, cancel);

    public static async Task Show(string title, string message) => await ShowCustomPopup(title, message, AppResources.Conferma);

    public static async Task Show(string title)
    {
        string defaultMessage = AppResources.Sviluppo;
        await ShowCustomPopup(title, defaultMessage, AppResources.Conferma);
    }

    // =========================================
    //  METODO POPUP DOPPIA SCELTA
    // =========================================

    public static async Task<bool> ShowConfirmation(
    string title,
    string message,
    string confirmText,
    string cancelText,
    bool isDestructive = false)
    {
        var popup = new ConfirmPopup(
            title,
            message,
            confirmText,
            cancelText,
            isDestructive);

        await Shell.Current.CurrentPage.ShowPopupAsync(popup);

        return await popup.Result;
    }

    // =========================================
    //  METODO POPUP ERRORE
    // =========================================

    public static async Task ShowError(string message)
    {
        string errorMessage = "Errore";
        await ShowCustomPopup(errorMessage, message, AppResources.Conferma);
    }

    // =========================================
    //  METODO POPUP ATTENZIONE
    // =========================================

    public static async Task ShowWarning(string message)
    {
        string warningTitle = "Attenzione";
        await ShowCustomPopup(warningTitle, message, AppResources.Conferma);
    }

    // =========================================
    //  METODO POPUP ELIMINAZIONE
    // =========================================

    public static async Task<bool> ShowDeleteConfirmation(string itemName = "")
    {
        string message = string.IsNullOrEmpty(itemName) ? "Sei sicuro di voler procedere con l'eliminazione??" : $"Sei sicuro di voler eliminare {itemName}?";
        return await ShowConfirmation(
            "Conferma Eliminazione",
            message,
            "Elimina",
            "Annulla",
            true);
    }
}
