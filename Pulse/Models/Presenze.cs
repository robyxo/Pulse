using System;
using System.Collections.Generic;

namespace Pulse.Models;

public partial class Presenze
{
    public int Id { get; set; }

    public int AllievoId { get; set; }

    public int LezioneId { get; set; }

    public string Data { get; set; } = null!;

    public int Presente { get; set; }

    public virtual Allievi Allievo { get; set; } = null!;

    public virtual Lezioni Lezione { get; set; } = null!;
}
