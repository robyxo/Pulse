using CommunityToolkit.Mvvm.ComponentModel;

namespace Pulse.DTO;

public partial class OpzioneColore : ObservableObject
{
    public string Hex { get; set; } = string.Empty;

    [ObservableProperty]
    private bool _isSelezionato;
}