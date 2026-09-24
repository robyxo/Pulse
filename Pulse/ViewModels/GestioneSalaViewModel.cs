using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pulse.DTO;
using Pulse.Models;
using Pulse.Services;
using System.Collections.ObjectModel;
using Pulse.Utils;

namespace Pulse.ViewModels;

[QueryProperty(nameof(Sala), "Sala")]
public partial class GestioneSalaViewModel : BaseViewModel
{
    private readonly IDatabaseService _dbService;

    [ObservableProperty]
    private Sale _sala = new();

    [ObservableProperty]
    private string _nome = string.Empty;

    [ObservableProperty]
    private string _descrizione = string.Empty;

    [ObservableProperty]
    private int _capienza;

    [ObservableProperty]
    private bool _isEdizione = false;

    private const string ColorePredefinito = "#0EA5E9";

    // Stessi colori della palette dei corsi, in un ordine che parte dai toni
    // piu' distinguibili fra loro.
    private static readonly string[] _palette =
    {
        "#0EA5E9", "#10B981", "#F59E0B", "#EF4444", "#8B5CF6", "#EC4899",
        "#14B8A6", "#F97316", "#84CC16", "#6366F1", "#64748B", "#E11D48"
    };

    [ObservableProperty]
    private string _coloreHex = ColorePredefinito;

    public ObservableCollection<OpzioneColore> PaletteColori { get; } = new(
        _palette.Select(c => new OpzioneColore { Hex = c, IsSelezionato = c == ColorePredefinito }));

    public GestioneSalaViewModel(IDatabaseService dbService)
    {
        _dbService = dbService;
        Title = "Gestione Sala";
    }

    partial void OnColoreHexChanged(string value)
    {
        foreach (var opzione in PaletteColori)
        {
            opzione.IsSelezionato = string.Equals(opzione.Hex, value, StringComparison.OrdinalIgnoreCase);
        }
    }

    [RelayCommand]
    public void SelezionaColore(string hex)
    {
        if (!string.IsNullOrWhiteSpace(hex)) ColoreHex = hex;
    }

    partial void OnSalaChanged(Sale value)
    {
        if (value == null) return;

        Nome = value.Nome ?? string.Empty;
        Descrizione = value.Descrizione ?? string.Empty;
        Capienza = value.Capienza ?? 0;
        ColoreHex = string.IsNullOrWhiteSpace(value.Colore) ? ColorePredefinito : value.Colore;

        IsEdizione = value.Id > 0;
    }

    [RelayCommand]
    public async Task SalvaSalaAsync()
    {
        if (string.IsNullOrWhiteSpace(Nome))
        {
            await AlertPopup.Show("Attenzione", "Inserisci il nome della sala.", "OK");
            return;
        }

        await EseguiConCaricamento(async () =>
        {
            Sala ??= new Sale();

            Sala.Nome = Nome.Trim();
            Sala.Descrizione = string.IsNullOrWhiteSpace(Descrizione) ? null : Descrizione.Trim();
            Sala.Capienza = Capienza > 0 ? Capienza : 0;

            Sala.Colore = string.IsNullOrWhiteSpace(ColoreHex) ? ColorePredefinito : ColoreHex;

            await _dbService.SalvaSalaAsync(Sala);
            await Shell.Current.Navigation.PopAsync();
        });
    }

    [RelayCommand]
    public async Task EliminaSalaAsync()
    {
        if (Sala == null || Sala.Id == 0) return;

        bool conferma = await AlertPopup.ShowConfirmation(
            "Conferma Eliminazione",
            $"Sei sicuro di voler eliminare la sala '{Sala.Nome}'?\n\nLe lezioni che la usano restano al loro posto, ma senza sala assegnata.",
            "Sì, Elimina",
            "Annulla");

        if (conferma)
        {
            await EseguiConCaricamento(async () =>
            {
                await _dbService.EliminaSalaAsync(Sala.Id);
                await Shell.Current.Navigation.PopAsync();
            });
        }
    }
}
