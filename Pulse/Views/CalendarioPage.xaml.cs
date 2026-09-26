using CommunityToolkit.Mvvm.Messaging;
using Pulse.DTO;
using Pulse.Models;

namespace Pulse.Views;

public partial class CalendarioPage : ContentPage
{
    private readonly CalendarioViewModel _viewModel;

    // Lista personalizzabile delle fasce orarie "a bande" (con orari di default)
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

        CaricaFasceSalvate();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        WeakReferenceMessenger.Default.Register<CalendarioViewModel.RefreshGridMessage>(this, (r, m) =>
        {
            MainThread.BeginInvokeOnMainThread(GeneraGriglia);
        });

        _ = _viewModel.CaricaDatiAsync();
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

    // ================================================
    // DISPATCHER: sceglie quale griglia disegnare
    // ================================================
    private void GeneraGriglia()
    {
        if (_viewModel.OrarioScaglionatoAttivo)
            GeneraGrigliaAOrario();
        else
            GeneraGrigliaABande();
    }

    // ================================================
    // INTESTAZIONE CONDIVISA
    // Prepara colonne, riga di testata e celle dei giorni.
    // Usata da entrambe le griglie: cambia solo il titolo della prima colonna.
    // Restituisce l'elenco dei giorni, che serve poi a chi costruisce le righe.
    // ================================================
    private List<DayOfWeek> PreparaGrigliaEIntestazione(string titoloPrimaColonna)
    {
        var grid = CalendarioGrid;
        grid.Children.Clear();
        grid.RowDefinitions.Clear();
        grid.ColumnDefinitions.Clear();

        var giorni = _viewModel.GiorniSettimana;

        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140, GridUnitType.Absolute) });
        for (int i = 0; i < giorni.Count; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        grid.Add(CreaCellaIntestazione(titoloPrimaColonna, isOggi: false), 0, 0);

        for (int i = 0; i < giorni.Count; i++)
        {
            var isOggi = DateTime.Today.DayOfWeek == giorni[i];

            grid.Add(CreaCellaIntestazione(_viewModel.GetNomeGiorno(giorni[i]), isOggi), i + 1, 0);
        }

        return giorni;
    }
    private static Border CreaCellaIntestazione(string testo, bool isOggi) => new Border
    {
        BackgroundColor = isOggi ? Color.FromArgb("#DBEAFE") : Color.FromArgb("#F8FAFC"),
        Padding = 10,
        HorizontalOptions = LayoutOptions.Fill,
        VerticalOptions = LayoutOptions.Fill,
        StrokeThickness = 0.5,
        Stroke = Color.FromArgb("#E2E8F0"),
        Content = new Label
        {
            Text = testo,
            FontAttributes = FontAttributes.Bold,
            FontSize = 12,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            TextColor = isOggi ? Color.FromArgb("#2563EB") : Color.FromArgb("#1E293B")
        }
    };

    // ================================================
    // GRIGLIA "A BANDE" (Mattina / Pomeriggio / Sera) — comportamento classico
    // ================================================


    private void GeneraGrigliaABande()
    {
        var grid = CalendarioGrid;
        var giorni = PreparaGrigliaEIntestazione("Fascia / Orario");

        var lezioni = _viewModel.LezioniSettimana;
        int rigaIndex = 1;

        foreach (var fascia in _fasceOrarie)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

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

            var tapModifica = new TapGestureRecognizer();
            var f = fascia;
            tapModifica.Tapped += async (_, _) =>
            {
                await PersonalizzaFasciaOraria(f);
            };
            cellaFascia.GestureRecognizers.Add(tapModifica);

            grid.Add(cellaFascia, 0, rigaIndex);

            for (int giornoIdx = 0; giornoIdx < giorni.Count; giornoIdx++)
            {
                var giornoCorrente = giorni[giornoIdx];
                int giornoDb = (int)giornoCorrente == 0 ? 7 : (int)giornoCorrente;

                var lezioniFascia = lezioni
                    .Where(l => l.GiornoSettimana == giornoDb &&
                                TimeSpan.TryParse(l.OraInizio, out var inizio) &&
                                inizio >= f.OraInizio && inizio < f.OraFine)
                    .OrderBy(l => TimeSpan.Parse(l.OraInizio))
                    .ToList();

                var cellaGiorno = CreaCellaGiornoConLezioni(lezioniFascia, giornoCorrente, f.OraInizio);
                grid.Add(cellaGiorno, giornoIdx + 1, rigaIndex);
            }

            rigaIndex++;
        }
    }

    // ================================================
    // GRIGLIA "A ORARIO" (fasce fisse ogni IntervalloMinuti) — Orario Scaglionato ON
    // ================================================
    private void GeneraGrigliaAOrario()
    {
        var grid = CalendarioGrid;
        var giorni = PreparaGrigliaEIntestazione("Orario");

        var lezioni = _viewModel.LezioniSettimana;
        var slotOrari = _viewModel.SlotOrari;
        int intervalloMinuti = _viewModel.IntervalloMinuti;
        int rigaIndex = 1;

        foreach (var oraInizioSlot in slotOrari)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var oraFineSlot = oraInizioSlot.Add(TimeSpan.FromMinutes(intervalloMinuti));

            var cellaOrario = new Border
            {
                BackgroundColor = Color.FromArgb("#EFF6FF"),
                Padding = 8,
                MinimumHeightRequest = 60,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
                Stroke = Color.FromArgb("#93C5FD"),
                StrokeThickness = 1,
                Content = new Label
                {
                    Text = oraInizioSlot.ToString(@"hh\:mm"),
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 12,
                    TextColor = Color.FromArgb("#1E40AF"),
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                }
            };
            grid.Add(cellaOrario, 0, rigaIndex);

            for (int giornoIdx = 0; giornoIdx < giorni.Count; giornoIdx++)
            {
                var giornoCorrente = giorni[giornoIdx];
                int giornoDb = (int)giornoCorrente == 0 ? 7 : (int)giornoCorrente;

                // Una lezione occupa lo slot se il suo intervallo si SOVRAPPONE allo slot,
                // non solo se inizia dentro lo slot: cosi' una lezione 09:00-10:00 con
                // intervallo da 30 minuti copre sia la riga delle 09:00 sia quella delle 09:30.
                var lezioniSlot = lezioni
                    .Where(l => l.GiornoSettimana == giornoDb &&
                                TimeSpan.TryParse(l.OraInizio, out var inizio) &&
                                TimeSpan.TryParse(l.OraFine, out var fine) &&
                                inizio < oraFineSlot && fine > oraInizioSlot)
                    .OrderBy(l => TimeSpan.Parse(l.OraInizio))
                    .ToList();

                // Le lezioni gia' iniziate in uno slot precedente vengono disegnate
                // come "proseguimento" (badge piu' tenue, senza ripetere l'orario).
                var idProseguimento = lezioniSlot
                    .Where(l => TimeSpan.TryParse(l.OraInizio, out var inizio) && inizio < oraInizioSlot)
                    .Select(l => l.Id)
                    .ToHashSet();

                var cellaGiorno = CreaCellaGiornoConLezioni(lezioniSlot, giornoCorrente, oraInizioSlot, idProseguimento);
                grid.Add(cellaGiorno, giornoIdx + 1, rigaIndex);
            }

            rigaIndex++;
        }
    }

    // ================================================
    // HELPER CONDIVISO: costruisce la cella di un giorno con i badge delle lezioni
    // (usato sia dalla griglia a bande che da quella a orario)
    // ================================================
    private Border CreaCellaGiornoConLezioni(List<Lezioni> lezioniCella, DayOfWeek giornoCorrente, TimeSpan oraCreazione, HashSet<int>? idProseguimento = null)
    {
        var cellaGiorno = new Border
        {
            BackgroundColor = Color.FromArgb("#FAFAFA"),
            Stroke = Color.FromArgb("#E2E8F0"),
            StrokeThickness = 0.5,
            MinimumHeightRequest = 60,
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

        foreach (var lezione in lezioniCella)
        {
            Color badgeColor;
            Color testoColor;
            if (_viewModel.ColoriPerSala)
            {
                // Colore della sala; grigio se la lezione non ha sala o la sala non ha colore.
                string? coloreSala = lezione.Sala?.Colore;
                badgeColor = string.IsNullOrWhiteSpace(coloreSala)
                    ? Color.FromArgb("#CBD5E1")
                    : _viewModel.StringToColor(coloreSala);
                testoColor = CalendarioViewModel.ColoreTestoLeggibile(badgeColor);
            }
            else
            {
                badgeColor = _viewModel.StringToColor(lezione.Corso?.Colore ?? "#4F46E5");
                testoColor = _viewModel.StringToColor(lezione.Corso?.ColoreTesto ?? "#FFFFFF");
            }

            bool proseguimento = idProseguimento?.Contains(lezione.Id) == true;

            // Tre righe, una sotto l'altra: Corso / Orario / Sala.
            // La sala e' facoltativa: se manca la riga non c'e'.
            var badgeLayout = new VerticalStackLayout
            {
                Spacing = 1,
                Children =
                {
                    new Label
                    {
                        Text = lezione.Corso?.Nome ?? "",
                        TextColor = testoColor,
                        FontSize = 11,
                        FontAttributes = FontAttributes.Bold,
                        LineBreakMode = LineBreakMode.TailTruncation
                    },
                    new Label
                    {
                        Text = proseguimento
                            ? $"↳ fino alle {lezione.OraFine}"
                            : $"🕒 {lezione.OraInizio} - {lezione.OraFine}",
                        TextColor = testoColor,
                        FontSize = 10
                    }
                }
            };

            // Nei proseguimenti la sala non si ripete: basta il rettangolo della prima riga.
            if (!proseguimento && !string.IsNullOrWhiteSpace(lezione.Sala?.Nome))
            {
                badgeLayout.Children.Add(new Label
                {
                    Text = $"🚪 {lezione.Sala!.Nome}",
                    TextColor = testoColor,
                    FontSize = 10,
                    LineBreakMode = LineBreakMode.TailTruncation
                });
            }

            var badge = new Border
            {
                BackgroundColor = badgeColor,
                Padding = 6,
                StrokeThickness = 0,
                Opacity = proseguimento ? 0.55 : 1,
                Content = badgeLayout
            };

            var lezioneCorrente = lezione;
            var tapModificaLezione = new TapGestureRecognizer();
            tapModificaLezione.Tapped += async (_, _) =>
            {
                await _viewModel.GestisciLezione(lezioneCorrente);
            };
            badge.GestureRecognizers.Add(tapModificaLezione);

            stackLezioni.Children.Add(badge);
        }

        var tapNuovo = new TapGestureRecognizer();
        tapNuovo.Tapped += async (_, _) =>
        {
            await _viewModel.CreaNuovaLezione(giornoCorrente, oraCreazione);
        };
        cellaGiorno.GestureRecognizers.Add(tapNuovo);

        cellaGiorno.Content = stackLezioni;
        return cellaGiorno;
    }

    // Le bande personalizzate vengono ricordate tra un'apertura e l'altra,
    // come già succede per i filtri Dalle / Alle / Intervallo del ViewModel.
    private void CaricaFasceSalvate()
    {
        foreach (var fascia in _fasceOrarie)
        {
            if (TimeSpan.TryParse(Preferences.Get($"Fascia_{fascia.Nome}_Inizio", string.Empty), out var inizio))
                fascia.OraInizio = inizio;

            if (TimeSpan.TryParse(Preferences.Get($"Fascia_{fascia.Nome}_Fine", string.Empty), out var fine))
                fascia.OraFine = fine;
        }
    }

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

        fascia.OraInizio = nuovaOraInizio;
        fascia.OraFine = nuovaOraFine;

        Preferences.Set($"Fascia_{fascia.Nome}_Inizio", nuovaOraInizio.ToString());
        Preferences.Set($"Fascia_{fascia.Nome}_Fine", nuovaOraFine.ToString());

        GeneraGriglia();
    }
}