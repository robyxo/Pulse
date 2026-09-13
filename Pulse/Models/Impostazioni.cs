using System;
using System.Collections.Generic;

namespace Pulse.Models;

public partial class Impostazioni
{
    public int Id { get; set; }

    public string? NomeScuola { get; set; }

    public string? IndirizzoScuola { get; set; }

    public string? PivaScuola { get; set; }

    public string? LogoPath { get; set; }

    public int? OrarioScaglionato { get; set; }

    public int? StampaRicevutaCortesia { get; set; }

    public int? StampaDocumentoPrivacy { get; set; }

    public int? NumeroColoriCorsi { get; set; }

    public string? EmailSmtpHost { get; set; }

    public int? EmailSmtpPort { get; set; }

    public string? EmailSmtpUser { get; set; }

    public string? EmailSmtpPassword { get; set; }

    public string? EmailMittenteNome { get; set; }

    public int? EmailUseSsl { get; set; }

    public string? BackupPath { get; set; }

    public string? UltimoBackupData { get; set; }

    public string? UltimoRipristinoData { get; set; }
}
