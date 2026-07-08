using System;
using System.Collections.Generic;

namespace Pulse.Models;

public partial class Lezioni
{
    public int Id { get; set; }

    public int CorsoId { get; set; }

    public int InsegnanteId { get; set; }

    public int GiornoSettimana { get; set; }

    public string OraInizio { get; set; } = null!;

    public string OraFine { get; set; } = null!;

    public string? Sala { get; set; }

    public string? Note { get; set; }

    public virtual Corsi Corso { get; set; } = null!;

    public virtual Insegnanti Insegnante { get; set; } = null!;

    public virtual ICollection<Presenze> Presenzes { get; set; } = new List<Presenze>();
}
