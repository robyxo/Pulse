using System;
using System.Collections.Generic;

namespace Pulse.Models;

public partial class Comunicazioni
{
    public int Id { get; set; }

    public DateTime DataInvio { get; set; }

    public string? Tipo { get; set; }

    public string? Oggetto { get; set; }

    public string? Corpo { get; set; }

    public int? AllievoId { get; set; }

    public string? Destinatario { get; set; }

    public int? Esito { get; set; }

    public string? MessaggioErrore { get; set; }

    public int? Attivo { get; set; }

    public virtual Allievi? Allievo { get; set; }
}
