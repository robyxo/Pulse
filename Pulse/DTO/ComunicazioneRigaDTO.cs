using Pulse.Models;

namespace Pulse.DTO;

/// <summary>
/// Riga dello storico comunicazioni. Una riga = un invio, anche quando i
/// destinatari sono molti: gli indirizzi restano consultabili aprendo la riga.
/// </summary>
public class ComunicazioneRigaDTO
{
    public Comunicazioni Comunicazione { get; set; } = null!;

    public int Id => Comunicazione.Id;

    public string DataTesto => Comunicazione.DataInvio.ToString("dd/MM/yyyy HH:mm");

    public string Tipo => string.IsNullOrWhiteSpace(Comunicazione.Tipo) ? "Email" : Comunicazione.Tipo;

    public string Oggetto => string.IsNullOrWhiteSpace(Comunicazione.Oggetto)
        ? "(senza oggetto)"
        : Comunicazione.Oggetto;

    public List<string> Destinatari => string.IsNullOrWhiteSpace(Comunicazione.Destinatario)
        ? new List<string>()
        : Comunicazione.Destinatario
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

    public int NumeroDestinatari => Destinatari.Count;

    public string DestinatariTesto => NumeroDestinatari switch
    {
        0 => "nessun destinatario",
        1 => Destinatari[0],
        _ => $"{NumeroDestinatari} destinatari"
    };

    public bool Riuscita => Comunicazione.Esito != 0;

    public string EsitoIcona => Riuscita ? "✅" : "❌";

    public string EsitoColore => Riuscita ? "#10B981" : "#EF4444";

    public string Anteprima
    {
        get
        {
            string corpo = (Comunicazione.Corpo ?? string.Empty).Replace("\n", " ").Trim();
            return corpo.Length <= 120 ? corpo : corpo[..120] + "…";
        }
    }
}
