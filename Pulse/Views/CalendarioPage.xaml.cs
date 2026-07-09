using Pulse.Models;
using Pulse.ViewModels;

namespace Pulse.Views;

public partial class CalendarioPage : ContentPage
{
    private readonly CalendarioViewModel _viewModel;
    private const int AltezzaRiga = 50;

    public CalendarioPage(CalendarioViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadData(); // aspetta i dati
        MainThread.BeginInvokeOnMainThread(GeneraGriglia); // ridisegna su UI thread
    }

    private void GeneraGriglia()
    {
        var grid = CalendarioGrid;
        grid.Children.Clear();
        grid.RowDefinitions.Clear();
        grid.ColumnDefinitions.Clear();

        // Colonne: orari + 7 giorni
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = 60 });
        for (int i = 0; i < 7; i++)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // Riga header
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.Add(new Label { Text = "Orari", FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.Center }, 0, 0);

        var giorni = _viewModel.GiorniSettimana;
        for (int i = 0; i < giorni.Count; i++)
        {
            var nome = _viewModel.GetNomeGiorno(giorni[i]);
            var isOggi = DateTime.Today.DayOfWeek == giorni[i] &&
                         DateTime.Today >= _viewModel.SettimanaCorrente.Date &&
                         DateTime.Today < _viewModel.SettimanaCorrente.Date.AddDays(7);

            grid.Add(new Label
            {
                Text = nome,
                FontAttributes = FontAttributes.Bold,
                FontSize = 12,
                HorizontalOptions = LayoutOptions.Center,
                TextColor = isOggi ? Color.FromArgb("#2563EB") : Colors.Black,
                BackgroundColor = isOggi ? Color.FromArgb("#DBEAFE") : Colors.Transparent,
                Padding = 8
            }, i + 1, 0);
        }

        // Righe orari
        var fasce = _viewModel.FasceOrarie;
        for (int slot = 0; slot < fasce.Count; slot++)
        {
            var fascia = fasce[slot];
            var riga = slot + 1;
            grid.RowDefinitions.Add(new RowDefinition { Height = AltezzaRiga });

            grid.Add(new Label
            {
                Text = fascia.Orario.ToString(@"hh\:mm"),
                FontSize = 11,
                TextColor = Color.FromArgb("#6B7280"),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            }, 0, riga);

            for (int giornoIdx = 0; giornoIdx < giorni.Count; giornoIdx++)
            {
                var lezione = _viewModel.GetLezionePerOrario(giorni[giornoIdx], fascia.Orario);
                var cella = new Border
                {
                    Stroke = Color.FromArgb("#E5E7EB"),
                    StrokeThickness = 0.5,
                    BackgroundColor = !fascia.IsAttiva
                       ? Color.FromArgb("#E5E7EB")
                        : lezione != null
                           ? _viewModel.StringToColor(lezione.Corso?.Colore ?? "#4F46E5")
                            : Colors.White
                };

                if (lezione != null && fascia.IsAttiva)
                {
                    var numAllievi = lezione.Corso?.Iscrizionis?.Count ?? 0;
                    var stack = new VerticalStackLayout
                    {
                        Padding = 4,
                        Spacing = 0,
                        Children =
                        {
                            new Label
                            {
                                Text = lezione.Corso?.Nome?? "",
                                FontSize = 11,
                                FontAttributes = FontAttributes.Bold,
                                TextColor = Colors.White,
                                MaxLines = 2,
                                LineBreakMode = LineBreakMode.TailTruncation
                            },
                            new Label
                            {
                                Text = $"👤 {numAllievi}",
                                FontSize = 9,
                                TextColor = Colors.White.WithAlpha(0.9f)
                            }
                        }
                    };
                    cella.Content = stack;

                    var tap = new TapGestureRecognizer();
                    tap.Tapped += async (_, _) => await _viewModel.MostraAllieviCorso(lezione);
                    cella.GestureRecognizers.Add(tap);

                    // RowSpan se la lezione dura più slot
                    var inizio = _viewModel.StringToTimeSpan(lezione.OraInizio);
                    var fine = _viewModel.StringToTimeSpan(lezione.OraFine);
                    var durata = fine - inizio;
                    var slotSpan = (int)(durata.TotalMinutes / _viewModel.IntervalloMinuti);
                    if (slotSpan > 1 && fascia.Orario == inizio)
                        Grid.SetRowSpan(cella, slotSpan);
                }

                if (fascia.IsAttiva || lezione != null)
                    grid.Add(cella, giornoIdx + 1, riga);
            }
        }
    }
}