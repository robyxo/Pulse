using Pulse.Models;

namespace Pulse.Helpers;

public enum StatoAbbonamento
{
    Nessuno,
    InPausa,
    Scaduto,
    InScadenza,
    Attivo,

    /// <summary>
    /// Registrato (per esempio dal tablet) ma senza nessun abbonamento:
    /// la segreteria deve ancora farglielo.
    /// </summary>
    DaAbbonare
}

public static class StatoAbbonamentoHelper
{
    /// <summary>Giorni entro i quali un abbonamento è considerato "in scadenza".</summary>
    public const int GiorniPreavvisoScadenza = 5;

    public static StatoAbbonamento Calcola(Abbonamenti? abbonamento)
    {
        if (abbonamento == null) return StatoAbbonamento.Nessuno;
        if (abbonamento.IsSospeso == 1) return StatoAbbonamento.InPausa;

        var oggi = DateTime.Today;
        var scadenza = abbonamento.DataScadenza.Date;

        if (oggi > scadenza) return StatoAbbonamento.Scaduto;
        if ((scadenza - oggi).TotalDays <= GiorniPreavvisoScadenza) return StatoAbbonamento.InScadenza;

        return StatoAbbonamento.Attivo;
    }

    /// <summary>
    /// Etichetta dello stato. Attenzione: questi testi sono anche le chiavi del
    /// filtro "Stato" nella pagina Allievi (StatiDisponibili in AllieviViewModel).
    /// </summary>
    public static string GetEtichetta(StatoAbbonamento stato) => stato switch
    {
        StatoAbbonamento.Attivo => "Attivo",
        StatoAbbonamento.InScadenza => "In Scadenza",
        StatoAbbonamento.InPausa => "In Pausa",
        StatoAbbonamento.Scaduto => "Scaduto",
        StatoAbbonamento.DaAbbonare => "Da Abbonare",
        _ => "Nessuno"
    };

    /// <summary>
    /// Ordine di urgenza: piu' basso = merita attenzione prima.
    /// Serve a scegliere, fra i corsi di uno stesso allievo, quale determina
    /// lo stato mostrato in tabella.
    /// </summary>
    public static int Priorita(StatoAbbonamento stato) => stato switch
    {
        StatoAbbonamento.Scaduto => 0,
        StatoAbbonamento.InScadenza => 1,
        StatoAbbonamento.InPausa => 2,
        StatoAbbonamento.Attivo => 3,
        _ => 4
    };

    public static string GetIcona(StatoAbbonamento stato) => stato switch
    {
        StatoAbbonamento.Attivo => "✅",
        StatoAbbonamento.InScadenza => "⏳",
        StatoAbbonamento.InPausa => "⏸️",
        StatoAbbonamento.DaAbbonare => "⚠️",
        _ => "❌"
    };

    public static string GetColore(StatoAbbonamento stato) => stato switch
    {
        StatoAbbonamento.Attivo => "#10B981",      // verde
        StatoAbbonamento.InScadenza => "#F59E0B",  // ambra
        StatoAbbonamento.InPausa => "#06B6D4",     // azzurro
        StatoAbbonamento.Scaduto => "#EF4444",     // rosso
        StatoAbbonamento.DaAbbonare => "#EAB308",  // giallo
        _ => "#94A3B8"                             // grigio
    };

    public static string GetEtichettaConIcona(StatoAbbonamento stato) =>
        $"{GetEtichetta(stato)} {GetIcona(stato)}";
}