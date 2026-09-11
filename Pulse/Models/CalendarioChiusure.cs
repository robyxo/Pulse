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
}
