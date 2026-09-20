using System.ComponentModel.DataAnnotations.Schema;

namespace Pulse.Models;

public partial class Allievi
{
    // Proprietà calcolata comoda per la visualizzazione nelle liste
    [NotMapped]
    public string NomeCompleto => $"{Nome} {Cognome}".Trim();
}