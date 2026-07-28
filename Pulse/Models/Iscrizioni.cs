using System;
using System.Collections.Generic;

namespace Pulse.Models;

public partial class Iscrizioni
{
    public int Id { get; set; }

    public int AllievoId { get; set; }

    public int CorsoId { get; set; }

    public string? TipoAbbonamento { get; set; }

    public string? DataInizio { get; set; }

    public string? DataScadenza { get; set; }

    public int? MesiTotali { get; set; }

    public int? MesiRimanenti { get; set; }

    public double? ImportoTotale { get; set; }

    public double? ImportoPagato { get; set; }

    public int? Attivo { get; set; }

    public virtual Allievi Allievo { get; set; } = null!;

    public virtual Corsi Corso { get; set; } = null!;
}
