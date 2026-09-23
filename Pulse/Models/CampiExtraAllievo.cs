using System;
using System.Collections.Generic;

namespace Pulse.Models;

public partial class CampiExtraAllievo
{
    public int Id { get; set; }

    public int AllievoId { get; set; }

    public string Chiave { get; set; } = null!;

    public string? Etichetta { get; set; }

    public string? Valore { get; set; }

    public DateTime? DataInserimento { get; set; }

    public string? Origine { get; set; }

    public int? Attivo { get; set; }

    public virtual Allievi Allievo { get; set; } = null!;
}
