using CommunityToolkit.Maui.Views;

namespace Pulse.Component;

public partial class ConfirmPopup : Popup
{
    private readonly TaskCompletionSource<bool> _tcs;

    public Task<bool> Result => 
        _tcs.Task;

    public ConfirmPopup(
        string title,
        string message,
        string confirmText,
        string cancelText = null,
        bool isDestructive = false)
    {
        InitializeComponent();

        _tcs = new TaskCompletionSource<bool>();

        TitleLabel.Text = title;
        MessageLabel.Text = message;
        ConfirmButton.Text = confirmText;
        CancelButton.Text = cancelText ?? "Annulla";

        ConfirmButton.BackgroundColor = isDestructive
            ? Colors.Red
            : Color.FromArgb("#2563EB");

        ConfirmButton.TextColor = Colors.White;
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        _tcs.TrySetResult(false);
        await CloseAsync();
    }

    private async void OnConfirmClicked(object sender, EventArgs e)
    {
        _tcs.TrySetResult(true);
        await CloseAsync();
    }
}
