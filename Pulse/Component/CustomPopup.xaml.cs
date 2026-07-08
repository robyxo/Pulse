using CommunityToolkit.Maui.Views;

namespace Pulse.Component;

public partial class CustomPopup : Popup
{
    public CustomPopup(string title, string message, string buttonText)
    {
        InitializeComponent();

        TitleLabel.Text = title;
        MessageLabel.Text = message;
        CloseButton.Text = buttonText;
    }

    private void OnCloseClicked(object sender, EventArgs e) => this.CloseAsync();
}
