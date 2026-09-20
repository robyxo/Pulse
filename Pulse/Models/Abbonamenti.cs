using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pulse.Models;

public partial class Abbonamenti
{
    public int Id { get; set; }

    public int AllievoId { get; set; }

    public int CorsoId { get; set; }

    public string TipoAbbonamento { get; set; } = null!;

    public DateTime DataInizio { get; set; }

    public DateTime DataScadenza { get; set; }

    public double ImportoTotale { get; set; }

    public double ImportoPagato { get; set; }

    public int IsPagato { get; set; }

    public int IsSospeso { get; set; }

    public DateTime? DataSospensione { get; set; }

    public int GiorniRimanentiCongelati { get; set; }

    public int Attivo { get; set; }

    public virtual Allievi Allievo { get; set; } = null!;

    public virtual Corsi Corso { get; set; } = null!;
}
