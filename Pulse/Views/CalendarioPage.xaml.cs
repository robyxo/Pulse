using CommunityToolkit.Mvvm.Messaging;
using Pulse.DTO;

namespace Pulse.Views;

public partial class CalendarioPage : ContentPage
{
    private readonly CalendarioViewModel _viewModel;

    // Lista personalizzabile delle fasce orarie (con orari di default)
    private readonly List<FasciaOraria> _fasceOrarie = new()
    {
        new FasciaOraria { Nome = "Mattina", OraInizio = new TimeSpan(9, 0, 0), OraFine = new TimeSpan(13, 0, 0) },
        new FasciaOraria { Nome = "Pomeriggio", OraInizio = new TimeSpan(13, 0, 0), OraFine = new TimeSpan(18, 0, 0) },
        new FasciaOraria { Nome = "Sera", OraInizio = new TimeSpan(18, 0, 0), OraFine = new TimeSpan(23, 0, 0) }
    };

    public CalendarioPage(CalendarioViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        WeakReferenceMessenger.Default.Register<CalendarioViewModel.RefreshGridMessage>(this, (r, m) =>
        {
            MainThread.BeginInvokeOnMainThread(GeneraGriglia);
        });

        _ = _viewModel.LoadData();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        WeakReferenceMessenger.Default.Unregister<CalendarioViewModel.RefreshGridMessage>(this);
    }

    private void OnToggleMenuClicked(object sender, EventArgs e)
    {
        MenuVoci.IsVisible = !MenuVoci.IsVisible;
    }

    private void GeneraGriglia()
    {
        var grid = CalendarioGrid;
        grid.Children.Clear();
        grid.RowDefinitions.Clear();
        grid.ColumnDefinitions.Clear();

        var giorni = _viewModel.GiorniSettimana;

        // 1. COLONNE: 140px per le Fasce Orarie + 7 colonne dei giorni a schermo intero (Star)
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140, GridUnitType.Absolute) });
        for (int i = 0; i < giorni.Count; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        // 2. RIGA HEADER GIORNI (Riga 0)
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        grid.Add(new Border
        {
            BackgroundColor = Color.FromArgb("#F8FAFC"),
            Padding = 10,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            StrokeThickness = 0.5,
            Stroke = Color.FromArgb("#E2E8F0"),
            Content = new Label
            {
                Text = "Fascia / Orario",
                FontAttributes = FontAttributes.Bold,
                FontSize = 12,
                TextColor = Color.FromArgb("#1E293B"),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            }
        }, 0, 0);

        for (int i = 0; i < giorni.Count; i++)
        {
            var nome = _viewModel.GetNomeGiorno(giorni[i]);
            var isOggi = DateTime.Today.DayOfWeek == giorni[i] &&
                         DateTime.Today >= _viewModel.SettimanaCorrente.Date &&
                         DateTime.Today < _viewModel.SettimanaCorrente.Date.AddDays(7);

            grid.Add(new Border
            {
                BackgroundColor = isOggi ? Color.FromArgb("#DBEAFE") : Color.FromArgb("#F8FAFC"),
                Padding = 10,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
                StrokeThickness = 0.5,
                Stroke = Color.FromArgb("#E2E8F0"),
                Content = new Label
                {
                    Text = nome,
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 12,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    TextColor = isOggi ? Color.FromArgb("#2563EB") : Color.FromArgb("#1E293B")
                }
            }, i + 1, 0);
        }

        // 3. GENERAZIONE RIGHE FASCE ORARIE
        var lezioni = _viewModel.LezioniSettimana;
        int rigaIndex = 1;

        foreach (var fascia in _fasceOrarie)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Cella della Fascia Oraria (Cliccabile per personalizzare)
            var cellaFascia = new Border
            {
                BackgroundColor = Color.FromArgb("#EFF6FF"),
                Padding = 8,
                MinimumHeightRequest = 100,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
                Stroke = Color.FromArgb("#93C5FD"),
                StrokeThickness = 1,
                Content = new VerticalStackLayout
                {
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    Spacing = 2,
                    Children =
                    {
                        new Label
                        {
                            Text = fascia.TitoloFormattato,
                            FontAttributes = FontAttributes.Bold,
                            FontSize = 11,
                            TextColor = Color.FromArgb("#1E40AF"),
                            HorizontalTextAlignment = TextAlignment.Center
                        },
                        new Label
                        {
                            Text = "✏️ Modifica",
                            FontSize = 9,
                            TextColor = Color.FromArgb("#3B82F6"),
                            HorizontalTextAlignment = TextAlignment.Center
                        }
                    }
                }
            };

            // Evento TAP per modificare gli orari della fascia
            var tapModifica = new TapGestureRecognizer();
            var f = fascia;
            tapModifica.Tapped += async (_, _) =>
            {
                await PersonalizzaFasciaOraria(f);
            };
            cellaFascia.GestureRecognizers.Add(tapModifica);

            grid.Add(cellaFascia, 0, rigaIndex);

            // Popoliamo i 7 giorni per questa fascia
            for (int giornoIdx = 0; giornoIdx < giorni.Count; giornoIdx++)
            {
                var giornoCorrente = giorni[giornoIdx];
                int giornoDb = (int)giornoCorrente == 0 ? 7 : (int)giornoCorrente;

                // Filtro e ordinamento per orario di inizio
                var lezioniFascia = lezioni
                    .Where(l => l.GiornoSettimana == giornoDb &&
                                TimeSpan.TryParse(l.OraInizio, out var inizio) &&
                                inizio >= f.OraInizio && inizio < f.OraFine)
                    .OrderBy(l => TimeSpan.Parse(l.OraInizio))
                    .ToList();

                var cellaGiorno = new Border
                {
                    BackgroundColor = Color.FromArgb("#FAFAFA"),
                    Stroke = Color.FromArgb("#E2E8F0"),
                    StrokeThickness = 0.5,
                    MinimumHeightRequest = 100,
                    HorizontalOptions = LayoutOptions.Fill,
                    VerticalOptions = LayoutOptions.Fill,
                    Padding = 4
                };

                var stackLezioni = new VerticalStackLayout
                {
                    Spacing = 4,
                    HorizontalOptions = LayoutOptions.Fill,
                    VerticalOptions = LayoutOptions.Fill
                };

                foreach (var lezione in lezioniFascia)
                {
                    var badgeColor = _viewModel.StringToColor(lezione.Corso?.Colore ?? "#4F46E5");
                    var testoColor = _viewModel.StringToColor(lezione.Corso?.ColoreTesto ?? "#FFFFFF");

                    var badgeLayout = new VerticalStackLayout
                    {
                        Spacing = 1,
                        Children =
        {
            new Label
            {
                Text = $"🕒 {lezione.OraInizio} - {lezione.OraFine}",
                TextColor = testoColor,
                FontSize = 10,
                FontAttributes = FontAttributes.Bold
            },
            new Label
            {
                Text = lezione.Corso?.Nome ?? "",
                TextColor = testoColor,
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                LineBreakMode = LineBreakMode.TailTruncation
            }
        }
                    };

                    var tapNuovo = new TapGestureRecognizer();
                    tapNuovo.Tapped += async (_, _) =>
                    {
                        await _viewModel.CreaNuovaLezione(giornoCorrente, fascia.OraInizio);
                    };
                    cellaGiorno.GestureRecognizers.Add(tapNuovo);

                    cellaGiorno.Content = stackLezioni;
                    grid.Add(cellaGiorno, giornoIdx + 1, rigaIndex);
                }

                rigaIndex++;
            }
        }
    }

    /// <summary>
    /// Popup per modificare l'orario di inizio e fine della fascia selezionata
    /// </summary>
    private async Task PersonalizzaFasciaOraria(FasciaOraria fascia)
    {
        string resInizio = await DisplayPromptAsync(
            $"Modifica {fascia.Nome}",
            "Inserisci l'orario di INIZIO (es. 09:00 o 14:30):",
            initialValue: fascia.OraInizio.ToString(@"hh\:mm"),
            keyboard: Keyboard.Numeric);

        if (string.IsNullOrWhiteSpace(resInizio) || !TimeSpan.TryParse(resInizio, out var nuovaOraInizio))
            return;
        string resFine = await DisplayPromptAsync(
            $"Modifica {fascia.Nome}",
            "Inserisci l'orario di FINE (es. 13:00 o 18:00):",
            initialValue: fascia.OraFine.ToString(@"hh\:mm"),
            keyboard: Keyboard.Numeric);

        if (string.IsNullOrWhiteSpace(resFine) || !TimeSpan.TryParse(resFine, out var nuovaOraFine))
            return;

        if (nuovaOraInizio >= nuovaOraFine)
        {
            await DisplayAlert("Orario Non Valido", "L'orario d'inizio deve essere precedente all'orario di fine.", "OK");
            return;
        }

        // Aggiorniamo la fascia e rigeneriamo la griglia con i nuovi filtri
        fascia.OraInizio = nuovaOraInizio;
        fascia.OraFine = nuovaOraFine;

        GeneraGriglia();
    }
}