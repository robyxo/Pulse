using System;
using System.Collections.Generic;

namespace Pulse.Models;

public partial class Stagioni
{
    public int Id { get; set; }

    public string Nome { get; set; } = null!;

    public DateTime DataInizio { get; set; }

    public DateTime DataFine { get; set; }

    public int? IsCorrente { get; set; }

    public string? Note { get; set; }

    public int? Attivo { get; set; }

    public virtual ICollection<CalendarioChiusure> CalendarioChiusures { get; set; } = new List<CalendarioChiusure>();

    public virtual ICollection<Impostazioni> Impostazionis { get; set; } = new List<Impostazioni>();
}
