using System;
using System.Collections.Generic;

namespace Pulse.Models;

public partial class Privacy
{
    public int Id { get; set; }

    public int? IdAllievo { get; set; }

    public string? Data { get; set; }

    public string? Conoscenza { get; set; }

    public string? Allegato { get; set; }

    public int? PresaVisione { get; set; }

    public int? Firmato { get; set; }

    public DateTime? DataFirma { get; set; }

    public string? PercorsoPdf { get; set; }

    public string? ModelloUsato { get; set; }

    public int? Attivo { get; set; }

    public virtual Allievi? IdAllievoNavigation { get; set; }
}
