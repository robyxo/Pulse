using System;
using System.Collections.Generic;

namespace Pulse.Models;

public partial class PagamentiInsegnanti
{
    public int Id { get; set; }

    public int InsegnanteId { get; set; }

    public DateTime DataPagamento { get; set; }

    public DateTime? PeriodoDal { get; set; }

    public DateTime? PeriodoAl { get; set; }

    public double? OreTotali { get; set; }

    public double Importo { get; set; }

    public string? MetodoPagamento { get; set; }

    public string? Note { get; set; }

    public int? Attivo { get; set; }

    public virtual Insegnanti Insegnante { get; set; } = null!;
}
