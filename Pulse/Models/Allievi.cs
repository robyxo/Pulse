using System;
using System.Collections.Generic;

namespace Pulse.Models;

public partial class Allievi
{
    public int Id { get; set; }

    public string Nome { get; set; } = null!;

    public string Cognome { get; set; } = null!;

    public string? CodiceFiscale { get; set; }

    public string? Telefono { get; set; }

    public string? Email { get; set; }

    public string? Allegati { get; set; }

    public DateTime? DataNascita { get; set; }

    public int? Attivo { get; set; }

    // Proprietà calcolata comoda per la visualizzazione nelle liste

    public string NomeCompleto => $"{Nome} {Cognome}";

    public virtual ICollection<Iscrizioni> Iscrizionis { get; set; } = new List<Iscrizioni>();

    public virtual ICollection<Presenze> Presenzes { get; set; } = new List<Presenze>();
}
