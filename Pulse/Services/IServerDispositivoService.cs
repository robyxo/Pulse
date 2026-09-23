namespace Pulse.Services;

/// <summary>
/// Piccolo server web interno per la registrazione degli allievi dal tablet.
///
/// Si accende con l'interruttore "Dispositivo" delle impostazioni e risponde
/// solo dentro la rete della scuola, all'indirizzo contenuto nel QR code:
///   http://192.168.1.xx:5000/registrazionePrivacy?k=CHIAVE  (con il modello dispositivo)
///   http://192.168.1.xx:5000/registrazione?k=CHIAVE         (senza modello: solo anagrafica)
/// </summary>
public interface IServerDispositivoService
{
    /// <summary>True se il server è acceso e aspetta il tablet.</summary>
    bool InAscolto { get; }

    /// <summary>Porta in uso (o che si userà).</summary>
    int Porta { get; }

    /// <summary>Motivo dell'ultimo avvio fallito, da mostrare nelle impostazioni. Null se tutto ok.</summary>
    string? UltimoErrore { get; }

    /// <summary>
    /// Accende o spegne il server in base alle impostazioni salvate.
    /// Da chiamare all'avvio dell'app e dopo ogni salvataggio delle impostazioni.
    /// </summary>
    Task AggiornaDaImpostazioniAsync();

    /// <summary>Spegne il server.</summary>
    void Ferma();

    /// <summary>
    /// Link completo da mettere nel QR code. Null se il server è spento o se il
    /// PC non risulta collegato a nessuna rete.
    /// </summary>
    Task<string?> GetLinkRegistrazioneAsync();

    /// <summary>
    /// Cambia la chiave: il QR stampato o fotografato prima smette di funzionare.
    /// Restituisce il nuovo link.
    /// </summary>
    Task<string?> RigeneraChiaveAsync();
}
