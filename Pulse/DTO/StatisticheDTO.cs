namespace Pulse.DTO;

/// <summary>
/// Tutto quello che mostra la pagina Statistiche per un periodo dal/al.
/// Viene calcolato in un colpo solo da IStatisticheService.
/// </summary>
public class RiepilogoStatisticheDTO
{
    public DateTime Dal { get; set; }
    public DateTime Al { get; set; }

    // --- Soldi ---

    /// <summary>Incassato dagli abbonamenti pagati nel periodo (esclusi gli stornati).</summary>
    public double Entrate { get; set; }

    /// <summary>Pagamenti ai maestri registrati nel periodo.</summary>
    public double Uscite { get; set; }

    public double Saldo => Entrate - Uscite;

    /// <summary>Quanto manca da incassare sugli abbonamenti iniziati nel periodo.</summary>
    public double DaIncassare { get; set; }

    /// <summary>Ore x tariffa dei maestri nel periodo, in base all'orario attuale.</summary>
    public double CompensiPrevisti { get; set; }

    // --- Allievi ---

    public int AbbonamentiVenduti { get; set; }

    /// <summary>Allievi con almeno un abbonamento valido in qualche giorno del periodo.</summary>
    public int AllieviAttivi { get; set; }

    /// <summary>Allievi il cui primo abbonamento in assoluto cade nel periodo.</summary>
    public int AllieviNuovi { get; set; }

    /// <summary>Registrati (es. dal tablet) che non hanno ancora un abbonamento.</summary>
    public int AllieviDaAbbonare { get; set; }

    // --- Dettagli ---

    public List<VoceGraficoDTO> EntratePerMese { get; set; } = new();
    public List<VoceGraficoDTO> Corsi { get; set; } = new();
    public List<VoceGraficoDTO> TipiAbbonamento { get; set; } = new();

    /// <summary>Lezioni del calendario con piu' iscritti (prime 5).</summary>
    public List<VoceGraficoDTO> LezioniPiuFrequentate { get; set; } = new();

    /// <summary>Lezioni del calendario con meno iscritti (ultime 5, dalla piu' vuota).</summary>
    public List<VoceGraficoDTO> LezioniMenoFrequentate { get; set; } = new();
    public List<VoceInsegnanteDTO> Insegnanti { get; set; } = new();
    public List<GruppoCampoExtraDTO> CampiExtra { get; set; } = new();
}

/// <summary>
/// Una riga con barra orizzontale: etichetta, valore e lunghezza della barra
/// (0-1, rispetto alla riga piu' grande dello stesso elenco).
/// </summary>
public class VoceGraficoDTO
{
    public string Etichetta { get; set; } = string.Empty;
    public double Valore { get; set; }
    public string ValoreTesto { get; set; } = string.Empty;
    public string? Dettaglio { get; set; }
    public double Proporzione { get; set; }
    public string Colore { get; set; } = "#4F46E5";

    public bool HaDettaglio => !string.IsNullOrWhiteSpace(Dettaglio);
}

public class VoceInsegnanteDTO
{
    public string Nome { get; set; } = string.Empty;
    public double Tariffa { get; set; }
    public double Ore { get; set; }
    public double CompensoPrevisto { get; set; }
    public double Pagato { get; set; }

    public double DaPagare => Math.Max(0, CompensoPrevisto - Pagato);

    public string OreTesto => $"{Ore:0.#} h";
    public string CompensoTesto => $"€ {CompensoPrevisto:N2}";
    public string PagatoTesto => $"€ {Pagato:N2}";
    public string DaPagareTesto => $"€ {DaPagare:N2}";
    public bool HaDaPagare => DaPagare > 0.009;
}

/// <summary>Risposte a una domanda extra del modulo (es. "Come ci hai conosciuto").</summary>
public class GruppoCampoExtraDTO
{
    public string Chiave { get; set; } = string.Empty;
    public string Etichetta { get; set; } = string.Empty;
    public int TotaleRisposte { get; set; }
    public List<VoceGraficoDTO> Risposte { get; set; } = new();

    public string Intestazione => $"{Etichetta} ({TotaleRisposte} risposte)";
}

/// <summary>Risultato di una query salvata, pronto da mostrare come tabella di testo.</summary>
public class RisultatoQueryDTO
{
    public List<string> Colonne { get; set; } = new();
    public List<string[]> Righe { get; set; } = new();
    public bool Troncato { get; set; }
    public string? Errore { get; set; }

    public bool Riuscita => Errore == null;
}
