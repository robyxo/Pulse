using System;
using System.Collections.Generic;

namespace Pulse.Models;

public partial class Iscrizioni
{
    public int Id { get; set; }

    public int AllievoId { get; set; }

    public int CorsoId { get; set; }

    public string DataIscrizione { get; set; } = null!;

    public int Attivo { get; set; }

    public virtual Allievi Allievo { get; set; } = null!;

    public virtual Corsi Corso { get; set; } = null!;
}
