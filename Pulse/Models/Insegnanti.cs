using System;
using System.Collections.Generic;

namespace Pulse.Models;

public partial class Insegnanti
{
    public int Id { get; set; }

    public string Nome { get; set; } = null!;

    public string Cognome { get; set; } = null!;

    public string? Telefono { get; set; }

    public string? Email { get; set; }

    public string? Specialita { get; set; }

    public int Attivo { get; set; }

    public virtual ICollection<Lezioni> Lezionis { get; set; } = new List<Lezioni>();
}
