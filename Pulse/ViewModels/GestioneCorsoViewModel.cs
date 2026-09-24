using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pulse.DTO;
using Pulse.Models;
using Pulse.Services;
using Pulse.Utils;
using System.Collections.ObjectModel;

namespace Pulse.ViewModels;

[QueryProperty(nameof(Corso), "Corso")]
public partial class GestioneCorsoViewModel : BaseViewModel
{
    private readonly IDatabaseService _dbService;
    private readonly IImpostazioniService _impostazioniService;

    // Palette "master": i primi N (in base a Impostazioni.NumeroColoriCorsi) vengono mostrati
    private static readonly List<string> _paletteMaster = new()
    {
        "#4F46E5", "#10B981", "#EF4444", "#F59E0B", "#EC4899", "#0EA5E9",
        "#8B5CF6", "#14B8A6", "#F97316", "#84CC16", "#06B6D4", "#A855F7",
        "#E11D48", "#22C55E", "#EAB308", "#6366F1", "#D946EF", "#0D9488",
        "#F43F5E", "#64748B"
    };

    [ObservableProperty]
    private Corsi _corso = new();

    [ObservableProperty]
    private string _nome = string.Empty;

    [ObservableProperty]
    private string _descrizione = string.Empty;

    [ObservableProperty]
    private string _coloreHex = "#4F46E5";

    [ObservableProperty]
    private string _coloreTestoHex = "#000000";

    [ObservableProperty]
    private ObservableCollection<OpzioneColore> _paletteColori = new();

    // 💶 PROPRIETÀ PREZZI ABBONAMENTO
    [ObservableProperty]
    private double? _costoSingolo;

    [ObservableProperty]
    private double? _costoMensile;

    [ObservableProperty]
    private double? _costoAnnuale;

    [ObservableProperty]
    private bool _isEdizione = false;

    public GestioneCorsoViewModel(IDatabaseService dbService, IImpostazioniService impostazioniService)
    {
        _dbService = dbService;
        _impostazioniService = impostazioniService;
        Title = "Gestione Corso";

        _ = CaricaPaletteColoriAsync();
    }

    private async Task CaricaPaletteColoriAsync()
    {
        var impostazioni = await _impostazioniService.GetImpostazioniAsync();
        int numero = Math.Clamp(impostazioni.NumeroColoriCorsi ?? 6, 1, _paletteMaster.Count);

        PaletteColori = new ObservableCollection<OpzioneColore>(
            _paletteMaster.Take(numero).Select(c => new OpzioneColore { Hex = c, IsSelezionato = c == ColoreHex }));
    }

    partial void OnColoreHexChanged(string value)
    {
        foreach (var opzione in PaletteColori)
        {
            opzione.IsSelezionato = opzione.Hex == value;
        }
    }

    partial void OnCorsoChanged(Corsi value)
    {
        if (value == null) return;

        Nome = value.Nome ?? string.Empty;
        Descrizione = value.Descrizione ?? string.Empty;
        ColoreHex = string.IsNullOrWhiteSpace(value.Colore) ? "#4F46E5" : value.Colore;

        CostoSingolo = value.CostoSingolo;
        CostoMensile = value.CostoMensile;
        CostoAnnuale = value.CostoAnnuale;

        IsEdizione = value.Id > 0;

        ColoreTestoHex = string.IsNullOrWhiteSpace(value.ColoreTesto) ? "#000000" : value.ColoreTesto;
    }

    [RelayCommand]
    public void SelezionaColorePreset(string hex)
    {
        if (!string.IsNullOrWhiteSpace(hex))
        {
            ColoreHex = hex;
        }
    }

    [RelayCommand]
    public void SelezionaColoreTesto(string hex)
    {
        if (!string.IsNullOrWhiteSpace(hex))
        {
            ColoreTestoHex = hex;
        }
    }

    [RelayCommand]
    public async Task SalvaCorsoAsync()
    {
        if (string.IsNullOrWhiteSpace(Nome))
        {
            await AlertPopup.Show("Attenzione", "Inserisci il nome del corso.", "OK");
            return;
        }

        await EseguiConCaricamento(async () =>
        {
            Corso ??= new Corsi();

            Corso.Nome = Nome.Trim();
            Corso.Descrizione = Descrizione?.Trim();
            Corso.Colore = string.IsNullOrWhiteSpace(ColoreHex) ? "#4F46E5" : ColoreHex.Trim();
            Corso.ColoreTesto = string.IsNullOrWhiteSpace(ColoreTestoHex) ? "#000000" : ColoreTestoHex.Trim();

            // Assegnazione Prezzi
            Corso.CostoSingolo = CostoSingolo ?? 0;
            Corso.CostoMensile = CostoMensile ?? 0;
            Corso.CostoAnnuale = CostoAnnuale ?? 0;

            await _dbService.SalvaCorsoAsync(Corso);
            await Shell.Current.Navigation.PopAsync();
        });
    }

    [RelayCommand]
    public async Task EliminaCorsoAsync()
    {
        if (Corso == null || Corso.Id == 0) return;

        bool conferma = await AlertPopup.ShowConfirmation(
            "Conferma Eliminazione",
            $"Sei sicuro di voler eliminare il corso '{Corso.Nome}'?",
            "Sì, Elimina",
            "Annulla");

        if (conferma)
        {
            await EseguiConCaricamento(async () =>
            {
                await _dbService.EliminaCorsoAsync(Corso.Id);
                await Shell.Current.Navigation.PopAsync();
            });
        }
    }
}