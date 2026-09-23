using System.Net;
using System.Text;
using Pulse.DTO;

namespace Pulse.Services;

/// <summary>
/// Pagine HTML mostrate sul tablet dal <see cref="ServerDispositivoService"/>.
/// Tutto in un solo file, senza risorse esterne: il tablet non ha bisogno di
/// internet, solo della rete della scuola.
/// </summary>
public static class PaginaRegistrazioneDispositivo
{
    /// <summary>Modulo di registrazione, con le eventuali domande extra.</summary>
    public static string Modulo(string? nomeScuola, IReadOnlyList<CampoExtraDTO> campiExtra)
    {
        string titolo = string.IsNullOrWhiteSpace(nomeScuola) ? "Registrazione" : nomeScuola.Trim();

        return ModelloModulo
            .Replace("{{TITOLO}}", Codifica(titolo))
            .Replace("{{DOMANDE_EXTRA}}", CostruisciDomandeExtra(campiExtra));
    }

    /// <summary>Pagina con un solo messaggio (link non valido, pagina inesistente, ...).</summary>
    public static string Messaggio(string titolo, string testo)
    {
        return ModelloMessaggio
            .Replace("{{TITOLO}}", Codifica(titolo))
            .Replace("{{TESTO}}", Codifica(testo));
    }

    private static string Codifica(string testo) => WebUtility.HtmlEncode(testo);

    /// <summary>
    /// Le domande a scelta diventano bottoni grandi da toccare, quelle libere
    /// una casella di testo. Una domanda che si chiama come un'altra più
    /// "_ALTRO" (es. CONOSCENZA_ALTRO) compare solo se nell'altra si sceglie ALTRO.
    /// </summary>
    private static string CostruisciDomandeExtra(IReadOnlyList<CampoExtraDTO> campi)
    {
        if (campi.Count == 0) return string.Empty;

        var html = new StringBuilder();
        html.Append("<div class=\"card\"><h2>Qualche domanda in più</h2>");

        foreach (var campo in campi)
        {
            string nome = "extra_" + campo.Chiave;
            string etichetta = Codifica(string.IsNullOrWhiteSpace(campo.Etichetta) ? campo.Chiave : campo.Etichetta);

            if (campo.IsScelta)
            {
                html.Append("<div class=\"domanda\"><label>").Append(etichetta).Append("</label><div class=\"scelte\">");
                foreach (var opzione in campo.Opzioni)
                {
                    string valore = Codifica(opzione);
                    html.Append("<label class=\"chip\"><input type=\"radio\" name=\"").Append(Codifica(nome))
                        .Append("\" value=\"").Append(valore).Append("\"><span>").Append(valore).Append("</span></label>");
                }
                html.Append("</div></div>");
                continue;
            }

            string? dipendeDa = null;
            if (campo.Chiave.EndsWith("_ALTRO", StringComparison.OrdinalIgnoreCase))
            {
                string principale = campo.Chiave[..^"_ALTRO".Length];
                bool haAltro = campi.Any(c => c.IsScelta
                                              && string.Equals(c.Chiave, principale, StringComparison.OrdinalIgnoreCase)
                                              && c.Opzioni.Any(o => string.Equals(o, "ALTRO", StringComparison.OrdinalIgnoreCase)));
                if (haAltro) dipendeDa = principale;
            }

            html.Append("<div class=\"domanda\"");
            if (dipendeDa != null) html.Append(" data-dipende=\"").Append(Codifica(dipendeDa)).Append('"');
            html.Append("><label for=\"").Append(Codifica(nome)).Append("\">").Append(etichetta).Append("</label>")
                .Append("<input type=\"text\" maxlength=\"200\" id=\"").Append(Codifica(nome))
                .Append("\" name=\"").Append(Codifica(nome)).Append("\"></div>");
        }

        html.Append("</div>");
        return html.ToString();
    }

    // ------------------------------------------------------------------
    // MODELLI HTML
    // ------------------------------------------------------------------

    private const string Stile = """
        <style>
            :root { --blu: #0EA5E9; --testo: #0F172A; --grigio: #64748B; --bordo: #CBD5E1; }
            * { box-sizing: border-box; }
            body { margin: 0; background: #F1F5F9; color: var(--testo); font-size: 18px;
                   font-family: system-ui, -apple-system, "Segoe UI", Roboto, Arial, sans-serif; }
            .contenitore { max-width: 760px; margin: 0 auto; padding: 20px 16px 40px; }
            h1 { font-size: 26px; margin: 6px 0 4px; }
            .sotto { color: var(--grigio); margin: 0 0 18px; }
            .card { background: #fff; border-radius: 14px; padding: 18px; margin-bottom: 16px;
                    box-shadow: 0 1px 3px rgba(0,0,0,.08); }
            .card h2 { font-size: 18px; margin: 0 0 14px; color: #334155; }
            .griglia { display: grid; grid-template-columns: 1fr 1fr; gap: 14px; }
            .intera { grid-column: 1 / -1; }
            label { display: block; font-size: 15px; font-weight: 600; color: #475569; margin-bottom: 5px; }
            .obbl::after { content: " *"; color: #DC2626; }
            input[type=text], input[type=email], input[type=tel], input[type=date], select {
                width: 100%; font-size: 18px; padding: 12px; border: 1px solid var(--bordo);
                border-radius: 10px; background: #fff; color: var(--testo); }
            input:focus, select:focus { outline: 2px solid var(--blu); border-color: var(--blu); }
            #codiceFiscale { text-transform: uppercase; letter-spacing: 1px; }
            .domanda { margin-bottom: 16px; }
            .scelte { display: flex; flex-wrap: wrap; gap: 10px; }
            .chip { margin: 0; }
            .chip input { position: absolute; opacity: 0; pointer-events: none; }
            .chip span { display: inline-block; padding: 12px 18px; border: 1px solid var(--bordo);
                         border-radius: 999px; font-weight: 600; color: #334155; background: #fff; }
            .chip input:checked + span { background: var(--blu); border-color: var(--blu); color: #fff; }
            .nota { color: var(--grigio); font-size: 15px; margin: 0 0 16px; }
            button { width: 100%; font-size: 20px; font-weight: 700; padding: 16px; border: 0;
                     border-radius: 12px; background: var(--blu); color: #fff; }
            button:disabled { opacity: .6; }
            .errore { display: none; background: #FEE2E2; color: #991B1B; padding: 14px;
                      border-radius: 10px; margin-bottom: 14px; font-weight: 600; }
            .conferma { position: fixed; inset: 0; background: rgba(15,23,42,.85); display: none;
                        align-items: center; justify-content: center; padding: 20px; }
            .conferma > div { background: #fff; border-radius: 16px; padding: 32px 24px;
                              text-align: center; max-width: 480px; }
            .conferma .icona { font-size: 56px; }
            .conferma h2 { margin: 8px 0; }
            @media (max-width: 560px) { .griglia { grid-template-columns: 1fr; } }
        </style>
        """;

    private static readonly string ModelloModulo = """
        <!DOCTYPE html>
        <html lang="it">
        <head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1">
        <title>Registrazione - {{TITOLO}}</title>
        """ + Stile + """
        </head>
        <body>
        <div class="contenitore">
            <h1>{{TITOLO}}</h1>
            <p class="sotto">Benvenuto! Compila i tuoi dati per registrarti.</p>

            <div id="errore" class="errore"></div>

            <form id="modulo" autocomplete="off" novalidate>
                <div class="card">
                    <h2>Dati personali</h2>
                    <div class="griglia">
                        <div><label for="cognome" class="obbl">Cognome</label>
                             <input type="text" id="cognome" name="cognome" maxlength="60" autocapitalize="words"></div>
                        <div><label for="nome" class="obbl">Nome</label>
                             <input type="text" id="nome" name="nome" maxlength="60" autocapitalize="words"></div>
                        <div class="intera"><label for="codiceFiscale" class="obbl">Codice fiscale</label>
                             <input type="text" id="codiceFiscale" name="codiceFiscale" maxlength="16" autocapitalize="characters" spellcheck="false"></div>
                        <div><label for="dataNascita">Data di nascita</label>
                             <input type="date" id="dataNascita" name="dataNascita"></div>
                        <div><label for="sesso">Sesso</label>
                             <select id="sesso" name="sesso">
                                 <option value=""></option>
                                 <option value="M">M</option>
                                 <option value="F">F</option>
                                 <option value="Altro">Altro</option>
                             </select></div>
                    </div>
                </div>

                <div class="card">
                    <h2>Residenza</h2>
                    <div class="griglia">
                        <div><label for="indirizzo">Indirizzo</label>
                             <input type="text" id="indirizzo" name="indirizzo" maxlength="120"></div>
                        <div><label for="civico">N°</label>
                             <input type="text" id="civico" name="civico" maxlength="10"></div>
                        <div><label for="cap">CAP</label>
                             <input type="text" id="cap" name="cap" maxlength="10" inputmode="numeric"></div>
                        <div><label for="citta">Città</label>
                             <input type="text" id="citta" name="citta" maxlength="80"></div>
                        <div><label for="provincia">Provincia</label>
                             <input type="text" id="provincia" name="provincia" maxlength="5" autocapitalize="characters"></div>
                    </div>
                </div>

                <div class="card">
                    <h2>Contatti</h2>
                    <div class="griglia">
                        <div><label for="cellulare">Cellulare</label>
                             <input type="tel" id="cellulare" name="cellulare" maxlength="30"></div>
                        <div><label for="telefono">Telefono</label>
                             <input type="tel" id="telefono" name="telefono" maxlength="30"></div>
                        <div class="intera"><label for="email">Email</label>
                             <input type="email" id="email" name="email" maxlength="120" autocapitalize="off" spellcheck="false"></div>
                    </div>
                </div>

                {{DOMANDE_EXTRA}}

                <p class="nota">Il modulo privacy da firmare te lo prepara la segreteria.</p>
                <button type="submit" id="invia">Registrati</button>
            </form>
        </div>

        <div id="conferma" class="conferma">
            <div>
                <div class="icona">&#9989;</div>
                <h2>Grazie <span id="nomeConferma"></span>!</h2>
                <p>Registrazione completata.<br>Passa in segreteria per l'abbonamento e la firma del modulo privacy.</p>
            </div>
        </div>

        <script>
        (function () {
            var chiave = new URLSearchParams(location.search).get('k') || '';
            var modulo = document.getElementById('modulo');
            var bottone = document.getElementById('invia');
            var riquadroErrore = document.getElementById('errore');
            var conferma = document.getElementById('conferma');
            var timerConferma = null;

            function mostraErrore(testo) {
                riquadroErrore.textContent = testo || '';
                riquadroErrore.style.display = testo ? 'block' : 'none';
                if (testo) window.scrollTo({ top: 0, behavior: 'smooth' });
            }

            // Stesso controllo che fa il PC: intercetta subito gli errori di battitura.
            var dispari = [1,0,5,7,9,13,15,17,19,21,2,4,18,20,11,3,6,8,12,14,16,10,22,25,24,23];
            function codiceFiscaleValido(cf) {
                if (!/^[A-Z]{6}[0-9LMNPQRSTUV]{2}[A-Z][0-9LMNPQRSTUV]{2}[A-Z][0-9LMNPQRSTUV]{3}[A-Z]$/.test(cf)) return false;
                var somma = 0;
                for (var i = 0; i < 15; i++) {
                    var c = cf.charCodeAt(i);
                    var indice = (c >= 48 && c <= 57) ? c - 48 : c - 65;
                    somma += (i % 2 === 0) ? dispari[indice] : indice;
                }
                return cf.charCodeAt(15) === 65 + (somma % 26);
            }

            // "Se altro, specifica" compare solo se si sceglie ALTRO.
            function aggiornaDomandeCollegate() {
                document.querySelectorAll('[data-dipende]').forEach(function (div) {
                    var scelta = document.querySelector('input[name="extra_' + div.getAttribute('data-dipende') + '"]:checked');
                    var visibile = !!scelta && scelta.value.toUpperCase() === 'ALTRO';
                    div.style.display = visibile ? '' : 'none';
                    if (!visibile) div.querySelectorAll('input').forEach(function (c) { c.value = ''; });
                });
            }

            function nuovaRegistrazione() {
                if (timerConferma) { clearTimeout(timerConferma); timerConferma = null; }
                conferma.style.display = 'none';
                modulo.reset();
                aggiornaDomandeCollegate();
                mostraErrore('');
                window.scrollTo(0, 0);
            }

            modulo.addEventListener('change', aggiornaDomandeCollegate);
            conferma.addEventListener('click', nuovaRegistrazione);
            aggiornaDomandeCollegate();

            modulo.addEventListener('submit', function (evento) {
                evento.preventDefault();
                mostraErrore('');

                var dati = { chiave: chiave, extra: {} };
                new FormData(modulo).forEach(function (valore, nome) {
                    if (nome.indexOf('extra_') === 0) dati.extra[nome.substring(6)] = valore;
                    else dati[nome] = valore;
                });

                dati.nome = (dati.nome || '').trim();
                dati.cognome = (dati.cognome || '').trim();
                dati.codiceFiscale = (dati.codiceFiscale || '').replace(/\s/g, '').toUpperCase();

                if (!dati.cognome) { mostraErrore('Scrivi il cognome.'); return; }
                if (!dati.nome) { mostraErrore('Scrivi il nome.'); return; }
                if (!codiceFiscaleValido(dati.codiceFiscale)) {
                    mostraErrore('Il codice fiscale non è valido: controlla di averlo scritto bene.');
                    return;
                }

                bottone.disabled = true;
                bottone.textContent = 'Invio in corso...';

                fetch('/api/registrazione', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(dati)
                })
                .then(function (r) { return r.json(); })
                .then(function (risposta) {
                    if (risposta.ok) {
                        document.getElementById('nomeConferma').textContent = dati.nome;
                        conferma.style.display = 'flex';
                        timerConferma = setTimeout(nuovaRegistrazione, 6000);
                    } else {
                        mostraErrore(risposta.messaggio || 'Registrazione non riuscita.');
                    }
                })
                .catch(function () {
                    mostraErrore('Impossibile contattare il computer della segreteria. Controlla di essere collegato al Wi-Fi della scuola.');
                })
                .finally(function () {
                    bottone.disabled = false;
                    bottone.textContent = 'Registrati';
                });
            });
        })();
        </script>
        </body>
        </html>
        """;

    private static readonly string ModelloMessaggio = """
        <!DOCTYPE html>
        <html lang="it">
        <head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1">
        <title>{{TITOLO}}</title>
        """ + Stile + """
        </head>
        <body>
        <div class="contenitore">
            <div class="card" style="text-align:center; margin-top: 40px;">
                <h1>{{TITOLO}}</h1>
                <p>{{TESTO}}</p>
            </div>
        </div>
        </body>
        </html>
        """;
}
