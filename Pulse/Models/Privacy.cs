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

    public virtual Allievi? IdAllievoNavigation { get; set; }
}
