using DocumentFormat.OpenXml.Packaging;
using Pulse.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Pulse.Services;

public class PrivacyService
{
    private readonly IDatabaseService _dbService;

    public PrivacyService(IDatabaseService dbService)
    {
        _dbService = dbService;
    }

    public static string GetCartellaPrivacy()
    {
        var cartella = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Privacy");
        if (!Directory.Exists(cartella))
            Directory.CreateDirectory(cartella);
        return cartella;
    }

    public async Task GeneraEStampaPrivacyAsync(Allievi allievo, bool compilato, string nomeScuola)
    {
        try
        {
            var cartellaModelli = GetCartellaPrivacy();

            // 🎯 SELEZIONE DEL MODELLO IN BASE ALLA MODALITÀ
            string nomeFileTemplate = compilato ? "AutoModelloPrivacy.docx" : "ModelloPrivacy.docx";
            var percorsoModello = Path.Combine(cartellaModelli, nomeFileTemplate);

            if (!File.Exists(percorsoModello))
            {
                var percorsoAppData = Path.Combine(FileSystem.AppDataDirectory, "Privacy", nomeFileTemplate);
                if (File.Exists(percorsoAppData))
                {
                    percorsoModello = percorsoAppData;
                }
                else
                {
                    await Shell.Current.DisplayAlert("Modello Non Trovato",
                        $"Il file '{nomeFileTemplate}' non è presente nella cartella:\n{cartellaModelli}\n\nAssicurati di aver inserito entrambi i modelli Word.", "OK");
                    return;
                }
            }

            var nomeFileOutput = compilato
                ? $"Privacy_{allievo.Cognome}_{allievo.Nome}_{DateTime.Now:yyyyMMdd_HHmmss}.docx"
                : $"Privacy_Modulo_Vuoto_{DateTime.Now:yyyyMMdd_HHmmss}.docx";

            var percorsoOutput = Path.Combine(FileSystem.CacheDirectory, nomeFileOutput);

            File.Copy(percorsoModello, percorsoOutput, overwrite: true);

            if (compilato)
            {
                SostituisciTagDocumento(percorsoOutput, allievo, nomeScuola);
            }

            // Registrazione evento privacy nel DB
            if (allievo.Id > 0)
            {
                var recordPrivacy = new Privacy
                {
                    IdAllievo = allievo.Id,
                    Data = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                    Conoscenza = compilato ? "Compilato Automatico" : "Modulo Cartaceo Vuoto",
                    Allegato = nomeFileOutput
                };
                await _dbService.SalvaPrivacyAsync(recordPrivacy);
            }

            // Invio alla condivisione/stampa di sistema
            await Launcher.Default.OpenAsync(new OpenFileRequest
            {
                Title = compilato ? $"Modulo Privacy - {allievo.NomeCompleto}" : "Modulo Privacy in Bianco",
                File = new ReadOnlyFile(percorsoOutput)
            });
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Errore Privacy", $"Impossibile elaborare il documento: {ex.Message}", "OK");
        }
    }

    private void SostituisciTagDocumento(string percorsoFile, Allievi a, string nomeScuola)
    {
        using var wordDoc = WordprocessingDocument.Open(percorsoFile, true);
        var mainPart = wordDoc.MainDocumentPart;
        if (mainPart == null) return;

        string sesso = (a.Sesso ?? "").Trim().ToUpper();
        string sessoVisualizzato = sesso.StartsWith("M") ? "[X] M    [ ] F" : (sesso.StartsWith("F") ? "[ ] M    [X] F" : "M / F");

        Dictionary<string, string> mappaTag = new Dictionary<string, string>
        {
            { "{{COGNOME}}", (a.Cognome ?? "").ToUpper() },
            { "{{NOME}}", (a.Nome ?? "").ToUpper() },
            { "{{CODICEFISCALE}}", (a.CodiceFiscale ?? "").ToUpper() },
            { "{{DATANASCITA}}", a.DataNascita ?? "" },
            { "{{SESSO}}", sessoVisualizzato },
            { "{{INDIRIZZO}}", a.Indirizzo ?? "" },
            { "{{CIVICO}}", a.NCivico ?? "" },
            { "{{CAP}}", a.Cap ?? "" },
            { "{{CITTA}}", a.Citta ?? "" },
            { "{{PROVINCIA}}", a.Provincia ?? "" },
            { "{{TELEFONO}}", a.Telefono ?? "" },
            { "{{CELLULARE}}", !string.IsNullOrWhiteSpace(a.Cellulare) ? a.Cellulare : (a.Telefono ?? "") },
            { "{{EMAIL}}", a.Email ?? "" },
            { "{{DATA_OGGI}}", DateTime.Now.ToString("dd/MM/yyyy") },
            { "{{NOME_SCUOLA}}", nomeScuola }
        };

        string docText;
        using (var reader = new StreamReader(mainPart.GetStream()))
        {
            docText = reader.ReadToEnd();
        }

        foreach (var kvp in mappaTag)
        {
            docText = docText.Replace(kvp.Key, System.Security.SecurityElement.Escape(kvp.Value));
        }

        using (var writer = new StreamWriter(mainPart.GetStream(FileMode.Create)))
        {
            writer.Write(docText);
        }

        mainPart.Document.Save();
    }
}