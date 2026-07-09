namespace Pulse.Helpers;

public static class DateHelper
{
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