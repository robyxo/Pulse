using System;
using System.Collections.Generic;

namespace Pulse.Models;

public partial class QuerySalvate
{
    public int Id { get; set; }

    public string Nome { get; set; } = null!;

    public string? Descrizione { get; set; }

    public string TestoQuery { get; set; } = null!;

    public string? Categoria { get; set; }

    public int? SoloAmministratore { get; set; }

    public int? Ordine { get; set; }

    public DateTime? UltimaEsecuzione { get; set; }

    public int? Attivo { get; set; }
}
