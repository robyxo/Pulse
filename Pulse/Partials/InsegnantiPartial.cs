using System.ComponentModel.DataAnnotations.Schema;

namespace Pulse.Models;

public partial class Insegnanti
{
    [NotMapped]
    public string NomeCompleto => $"{Nome} {Cognome}".Trim();
}