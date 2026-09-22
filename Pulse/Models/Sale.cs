using System;
using System.Collections.Generic;

namespace Pulse.Models;

public partial class Sale
{
    public int Id { get; set; }

    public string Nome { get; set; } = null!;

    public string? Descrizione { get; set; }

    public int? Capienza { get; set; }

    public string? Colore { get; set; }

    public int? Attivo { get; set; }

    public virtual ICollection<Lezioni> Lezionis { get; set; } = new List<Lezioni>();
}
