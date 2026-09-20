using Pulse.Helpers;
using Pulse.Models;

namespace Pulse.DTO;

public class AllievoTabellaDTO
{
    public Allievi Allievo { get; set; } = null!;
    public Abbonamenti? UltimoAbbonamento { get; set; }

    public string NomeCompleto => $"{Allievo.Nome} {Allievo.Cognome}".Trim();
    public string Telefono => !string.IsNullOrWhiteSpace(Allievo.Telefono) ? Allievo.Telefono : "-";
    public string CorsoNome => UltimoAbbonamento?.Corso?.Nome ?? "Nessun Corso";
    public string TipoAbbonamento => UltimoAbbonamento?.TipoAbbonamento ?? "-";
    public string ScadenzaTesto => UltimoAbbonamento != null ? UltimoAbbonamento.DataScadenza.ToString("dd/MM/yyyy") : "-";

    public StatoAbbonamento Stato => StatoAbbonamentoHelper.Calcola(UltimoAbbonamento);

    public string StatoChiave => StatoAbbonamentoHelper.GetEtichetta(Stato);

    public string ColorePallinoHex => StatoAbbonamentoHelper.GetColore(Stato);
}