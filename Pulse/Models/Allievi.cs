using System;
using System.Collections.Generic;

namespace Pulse.Models;

public partial class Allievi
{
    public int Id { get; set; }

    public string Nome { get; set; } = null!;

    public string Cognome { get; set; } = null!;

    public string? Indirizzo { get; set; }

    public string? NCivico { get; set; }

    public string? Cap { get; set; }

    public string? Citta { get; set; }

    public string? Provincia { get; set; }

    public string? Telefono { get; set; }

    public string? Sesso { get; set; }

    public string? DataNascita { get; set; }

    public string? Cellulare { get; set; }

    public string? CodiceFiscale { get; set; }

    public string? Email { get; set; }

    public string? Allegati { get; set; }

    public int? Attivo { get; set; }

    public int? DaAbbonare { get; set; }

    public DateTime? DataRegistrazione { get; set; }

    public string? Origine { get; set; }

    public virtual ICollection<Abbonamenti> Abbonamentis { get; set; } = new List<Abbonamenti>();

    public virtual ICollection<Comunicazioni> Comunicazionis { get; set; } = new List<Comunicazioni>();

    public virtual ICollection<Iscrizioni> Iscrizionis { get; set; } = new List<Iscrizioni>();

    public virtual ICollection<Presenze> Presenzes { get; set; } = new List<Presenze>();

    public virtual ICollection<Privacy> Privacies { get; set; } = new List<Privacy>();
}
