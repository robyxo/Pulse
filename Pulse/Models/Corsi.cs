using System;
using System.Collections.Generic;

namespace Pulse.Models;

public partial class Corsi
{
    public int Id { get; set; }

    public string Nome { get; set; } = null!;

    public string? Livello { get; set; }

    public string? Descrizione { get; set; }

    public string? Colore { get; set; }

    public double? CostoSingolo { get; set; }

    public double? CostoMensile { get; set; }

    public double? CostoAnnuale { get; set; }

    public int? Attivo { get; set; }

    public string? ColoreTesto { get; set; }

    public virtual ICollection<Abbonamenti> Abbonamentis { get; set; } = new List<Abbonamenti>();

    public virtual ICollection<Iscrizioni> Iscrizionis { get; set; } = new List<Iscrizioni>();

    public virtual ICollection<Lezioni> Lezionis { get; set; } = new List<Lezioni>();
}
