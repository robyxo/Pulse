using System;
using System.Collections.Generic;

namespace Pulse.Models;

public partial class CalendarioChiusure
{
    public int Id { get; set; }

    public string? DataInizio { get; set; }

    public string? DataFine { get; set; }

    public int? Stato { get; set; }

    public string? Motivo { get; set; }

    public string? Tipo { get; set; }

    public int? EmailInviata { get; set; }

    public int? Recupero { get; set; }

    public int? StagioneId { get; set; }

    public virtual Stagioni? Stagione { get; set; }
}
