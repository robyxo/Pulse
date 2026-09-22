using System;
using System.Collections.Generic;

namespace Pulse.Models;

public partial class DispositiviAutorizzati
{
    public int Id { get; set; }

    public string Nome { get; set; } = null!;

    public string Token { get; set; } = null!;

    public string? IndirizzoIp { get; set; }

    public DateTime? UltimoAccesso { get; set; }

    public int? Attivo { get; set; }
}
