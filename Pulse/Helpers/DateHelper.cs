namespace Pulse.Helpers;

public static class DateHelper
{
    // Nomi dei giorni nell'ordine usato dal database: 1 = Lunedì ... 7 = Domenica
    private static readonly string[] _giorniSettimana =
    {
        "Lunedì", "Martedì", "Mercoledì", "Giovedì", "Venerdì", "Sabato", "Domenica"
    };

    public static IReadOnlyList<string> GiorniSettimana => _giorniSettimana;

    /// <summary>
    /// Nome del giorno a partire dal valore salvato nel DB (1 = Lunedì ... 7 = Domenica).
    /// Se il valore è fuori intervallo restituisce <paramref name="seNonValido"/>.
    /// </summary>
    public static string GetNomeGiornoDaDb(int giornoDb, string seNonValido = "") =>
        giornoDb >= 1 && giornoDb <= 7 ? _giorniSettimana[giornoDb - 1] : seNonValido;

    public static string GetGiornoIT(DayOfWeek day)
    {
        return day switch
        {
            DayOfWeek.Monday => "Lun",
            DayOfWeek.Tuesday => "Mar",
            DayOfWeek.Wednesday => "Mer",
            DayOfWeek.Thursday => "Gio",
            DayOfWeek.Friday => "Ven",
            DayOfWeek.Saturday => "Sab",
            DayOfWeek.Sunday => "Dom",
            _ => ""
        };
    }

    public static string GetNomeGiornoCompletoIT(DayOfWeek giorno, DateTime dataRiferimento)
    {
        var data = dataRiferimento.Date;
        while (data.DayOfWeek != DayOfWeek.Monday) data = data.AddDays(-1);
        data = data.AddDays((int)giorno - (int)DayOfWeek.Monday);
        return $"{GetGiornoIT(giorno)} {data:dd}";
    }
}