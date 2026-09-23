using Pulse.Helpers;
using Pulse.Models;

namespace Pulse.DTO;

/// <summary>
/// Riga della tabella allievi.
///
/// Un allievo puo' frequentare piu' corsi, quindi non basta tenere un solo
/// abbonamento: lo stato mostrato deve essere quello del corso che merita
/// attenzione per primo, altrimenti un abbonamento in scadenza resta invisibile
/// perche' coperto da un altro corso ancora valido.
/// </summary>
public class AllievoTabellaDTO
{
    public Allievi Allievo { get; set; } = null!;

    /// <summary>
    /// Ultimo abbonamento di OGNI corso dell'allievo (uno per corso).
    /// </summary>
    public List<Abbonamenti> AbbonamentiPerCorso { get; set; } = new();

    public string NomeCompleto => $"{Allievo.Nome} {Allievo.Cognome}".Trim();

    public string Telefono => !string.IsNullOrWhiteSpace(Allievo.Telefono) ? Allievo.Telefono : "-";

    public int NumeroCorsi => AbbonamentiPerCorso.Select(a => a.CorsoId).Distinct().Count();

    /// <summary>
    /// L'abbonamento che determina lo stato della riga: il piu' urgente fra
    /// tutti i corsi e, a parita' di urgenza, quello che scade prima.
    /// </summary>
    public Abbonamenti? AbbonamentoRilevante => AbbonamentiPerCorso
        .OrderBy(a => StatoAbbonamentoHelper.Priorita(StatoAbbonamentoHelper.Calcola(a)))
        .ThenBy(a => a.DataScadenza)
        .FirstOrDefault();

    /// <summary>
    /// Registrato (per esempio dal tablet) e ancora senza nessun abbonamento.
    /// Appena la segreteria ne crea uno, il segno viene tolto in automatico.
    /// </summary>
    public bool IsDaAbbonare => Allievo.DaAbbonare == 1 && AbbonamentiPerCorso.Count == 0;

    public StatoAbbonamento Stato => IsDaAbbonare
        ? StatoAbbonamento.DaAbbonare
        : StatoAbbonamentoHelper.Calcola(AbbonamentoRilevante);

    /// <summary>Nome del corso se e' uno solo, altrimenti quanti sono.</summary>
    public string CorsoNome => NumeroCorsi switch
    {
        0 => "Nessun Corso",
        1 => AbbonamentiPerCorso[0].Corso?.Nome ?? "Nessun Corso",
        _ => $"{NumeroCorsi} corsi"
    };

    /// <summary>
    /// Riga piccola sotto il corso: la tipologia quando il corso e' uno solo,
    /// l'elenco dei corsi quando sono piu' di uno.
    /// </summary>
    public string TipoAbbonamento => NumeroCorsi switch
    {
        0 => "-",
        1 => AbbonamentiPerCorso[0].TipoAbbonamento,
        _ => string.Join(", ", AbbonamentiPerCorso
                .Select(a => a.Corso?.Nome)
                .Where(n => !string.IsNullOrWhiteSpace(n)))
    };

    /// <summary>
    /// Scadenza dell'abbonamento che determina lo stato. Con piu' corsi viene
    /// indicato anche quale, altrimenti la data non direbbe cosa rinnovare.
    /// </summary>
    public string ScadenzaTesto
    {
        get
        {
            var rilevante = AbbonamentoRilevante;
            if (rilevante == null) return "-";

            string data = rilevante.DataScadenza.ToString("dd/MM/yyyy");

            return NumeroCorsi > 1 && !string.IsNullOrWhiteSpace(rilevante.Corso?.Nome)
                ? $"{rilevante.Corso!.Nome} · {data}"
                : data;
        }
    }

    /// <summary>Chiave del filtro Stato: non contiene icone, va confrontata col filtro.</summary>
    public string StatoChiave => StatoAbbonamentoHelper.GetEtichetta(Stato);

    /// <summary>
    /// Testo mostrato in tabella: uguale alla chiave, tranne il triangolo giallo
    /// per chi è da abbonare, che deve saltare all'occhio.
    /// </summary>
    public string StatoVisualizzato => IsDaAbbonare ? $"⚠️ {StatoChiave}" : StatoChiave;

    /// <summary>Per distinguere due omonimi nel promemoria.</summary>
    public string NomeConRegistrazione => Allievo.DataRegistrazione.HasValue
        ? $"{NomeCompleto} · registrato il {Allievo.DataRegistrazione.Value:dd/MM HH:mm}"
        : NomeCompleto;

    public string ColorePallinoHex => StatoAbbonamentoHelper.GetColore(Stato);

    /// <summary>Usato dal filtro per corso: vale per QUALSIASI corso dell'allievo.</summary>
    public bool FrequentaCorso(int corsoId) => AbbonamentiPerCorso.Any(a => a.CorsoId == corsoId);
}
