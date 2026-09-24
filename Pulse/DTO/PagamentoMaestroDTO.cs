using Pulse.Models;

namespace Pulse.DTO;

/// <summary>Riga dello storico pagamenti nella scheda del maestro.</summary>
public class PagamentoMaestroDTO
{
    public PagamentiInsegnanti Pagamento { get; }

    public PagamentoMaestroDTO(PagamentiInsegnanti pagamento)
    {
        Pagamento = pagamento;
    }

    public string DataTesto => Pagamento.DataPagamento.ToString("dd/MM/yyyy");

    public string PeriodoTesto =>
        Pagamento.PeriodoDal is { } dal
            ? System.Globalization.CultureInfo.GetCultureInfo("it-IT").TextInfo.ToTitleCase(
                dal.ToString("MMMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("it-IT")))
            : "-";

    public string OreTesto => Pagamento.OreTotali is > 0 ? $"{Pagamento.OreTotali:0.#} h" : string.Empty;

    public string ImportoTesto => $"€ {Pagamento.Importo:N2}";

    public bool HaNote => !string.IsNullOrWhiteSpace(Pagamento.Note);

    public string Note => Pagamento.Note ?? string.Empty;
}
