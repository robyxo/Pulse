using Pulse.Helpers;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pulse.Models;

// Proprietà calcolate per la grafica.
// Stanno QUI e non in Models/Abbonamenti.cs perché quel file viene rigenerato
// dallo scaffold con --force, che cancella tutto ciò che non viene dal database.
public partial class Abbonamenti
{
    [NotMapped]
    public double DaPagare => Math.Max(0, ImportoTotale - ImportoPagato);

    [NotMapped]
    public StatoAbbonamento Stato => StatoAbbonamentoHelper.Calcola(this);

    [NotMapped]
    public string StatoTesto => StatoAbbonamentoHelper.GetEtichettaConIcona(Stato);

    [NotMapped]
    public string ColoreStatoHex => StatoAbbonamentoHelper.GetColore(Stato);

    [NotMapped]
    public string IconaPausaTesto => IsSospeso == 1 ? "▶️" : "⏸️";
}