using Pulse.Models;
using Pulse.ViewModels;

namespace Pulse.Views;

public partial class CalendarioPage : ContentPage
{
    private readonly CalendarioViewModel _viewModel;
    private const int AltezzaRiga = 40;

    public CalendarioPage(CalendarioViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

        Loaded += OnPageLoaded;
    }

    private async void OnPageLoaded(object? sender, EventArgs e)
    {
        await _viewModel.LoadData();
        GeneraGriglia();

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CalendarioViewModel.LezioniSettimana) ||
            e.PropertyName == nameof(CalendarioViewModel.SettimanaCorrente))
        {
            GeneraGriglia();
        }
    }

    private void GeneraGriglia()
    {
        var grid = CalendarioGrid;
        grid.Children.Clear();
        grid.RowDefinitions.Clear();
        grid.ColumnDefinitions.Clear();

        // 1. COLONNE
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        for (int i = 0; i < 7; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        // 2. ORARI
        var oraInizio = _viewModel.OraInizio;
        var oraFine = _viewModel.OraFine;
        var intervallo = TimeSpan.FromMinutes(_viewModel.IntervalloMinuti);
        var totaleSlot = (int)((oraFine - oraInizio).TotalMinutes / _viewModel.IntervalloMinuti);

        // 3. HEADER
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var emptyHeader = new Label
        {
            Text = "Orari",
            FontAttributes = FontAttributes.Bold,
            FontSize = 12,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Padding = new Thickness(5)
        };
        Grid.SetRow(emptyHeader, 0);
        Grid.SetColumn(emptyHeader, 0);
        grid.Children.Add(emptyHeader);

        var giorni = _viewModel.GiorniSettimana;
        for (int i = 0; i < giorni.Count; i++)
        {
            var giorno = giorni[i];
            var dataGiorno = _viewModel.SettimanaCorrente.Date;
            while (dataGiorno.DayOfWeek != DayOfWeek.Monday)
            {
                dataGiorno = dataGiorno.AddDays(-1);
            }
            dataGiorno = dataGiorno.AddDays(i);

            var isOggi = dataGiorno.Date == DateTime.Now.Date;

            var headerStack = new VerticalStackLayout
            {
                Spacing = 2,
                Padding = new Thickness(5, 8),
                BackgroundColor = isOggi ? Color.FromArgb("#E0F2FE") : Colors.Transparent,
                Children =
                {
                    new Label
                    {
                        Text = giorno.ToString().Substring(0, 3),
                        FontAttributes = FontAttributes.Bold,
                        FontSize = 12,
                        HorizontalOptions = LayoutOptions.Center,
                        TextColor = isOggi ? Color.FromArgb("#2563EB") : Colors.Black
                    },
                    new Label
                    {
                        Text = dataGiorno.Day.ToString(),
                        FontSize = 14,
                        HorizontalOptions = LayoutOptions.Center,
                        TextColor = isOggi ? Color.FromArgb("#2563EB") : Colors.Gray
                    }
                }
            };

            Grid.SetRow(headerStack, 0);
            Grid.SetColumn(headerStack, i + 1);
            grid.Children.Add(headerStack);
        }

        // 4. RIGHE ORARI
        for (int slot = 0; slot < totaleSlot; slot++)
        {
            var orario = oraInizio.Add(TimeSpan.FromMinutes(slot * _viewModel.IntervalloMinuti));
            var rigaIndex = slot + 1;

            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(AltezzaRiga) });

            // Orario
            var labelOrario = new Label
            {
                Text = orario.ToString(@"hh\:mm"),
                FontSize = 10,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                TextColor = Color.FromArgb("#6B7280"),
                Padding = new Thickness(5, 0)
            };
            Grid.SetRow(labelOrario, rigaIndex);
            Grid.SetColumn(labelOrario, 0);
            grid.Children.Add(labelOrario);

            // Giorni
            for (int giornoIdx = 0; giornoIdx < giorni.Count; giornoIdx++)
            {
                var giorno = giorni[giornoIdx];
                var lezione = _viewModel.GetLezionePerOrario(giorno, orario);

                var cella = new Frame
                {
                    BackgroundColor = lezione != null
                        ? _viewModel.StringToColor(lezione.Corso?.Colore ?? "#4F46E5")
                        : Color.FromArgb("#F9FAFB"),
                    BorderColor = Color.FromArgb("#E5E7EB"),
                    CornerRadius = 4,
                    Padding = new Thickness(2),
                    Margin = new Thickness(1),
                    HeightRequest = AltezzaRiga
                };

                if (lezione != null)
                {
                    var numAllievi = lezione.Corso?.Iscrizionis?.Count ?? 0;

                    var stack = new VerticalStackLayout
                    {
                        Spacing = 0,
                        Children =
                        {
                            new Label
                            {
                                Text = lezione.Corso?.Nome ?? "Corso",
                                FontSize = 10,
                                FontAttributes = FontAttributes.Bold,
                                TextColor = Colors.White,
                                HorizontalOptions = LayoutOptions.Center,
                                LineBreakMode = LineBreakMode.WordWrap,
                                MaxLines = 2
                            },
                            new Label
                            {
                                Text = $"👤 {numAllievi}",
                                FontSize = 8,
                                TextColor = Colors.White.WithAlpha(0.8f),
                                HorizontalOptions = LayoutOptions.Center
                            }
                        }
                    };

                    cella.Content = stack;

                    var tapGesture = new TapGestureRecognizer();
                    tapGesture.Tapped += async (s, e) =>
                    {
                        await _viewModel.MostraAllieviCorso(lezione);
                    };
                    cella.GestureRecognizers.Add(tapGesture);

                    // RowSpan per lezioni che occupano più slot
                    var oraInizioLezione = _viewModel.StringToTimeSpan(lezione.OraInizio);
                    var oraFineLezione = _viewModel.StringToTimeSpan(lezione.OraFine);
                    var durata = oraFineLezione - oraInizioLezione;
                    var slotOccupati = (int)(durata.TotalMinutes / _viewModel.IntervalloMinuti);
                    if (slotOccupati > 1)
                    {
                        Grid.SetRowSpan(cella, slotOccupati);
                    }
                }
                else
                {
                    cella.Content = null;
                }

                Grid.SetRow(cella, rigaIndex);
                Grid.SetColumn(cella, giornoIdx + 1);
                grid.Children.Add(cella);
            }
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }
    }
}