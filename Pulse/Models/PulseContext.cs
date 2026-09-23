using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Pulse.Models;

public partial class PulseContext : DbContext
{
    public PulseContext(DbContextOptions<PulseContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Abbonamenti> Abbonamentis { get; set; }

    public virtual DbSet<Allievi> Allievis { get; set; }

    public virtual DbSet<CalendarioChiusure> CalendarioChiusures { get; set; }

    public virtual DbSet<CampiExtraAllievo> CampiExtraAllievos { get; set; }

    public virtual DbSet<Comunicazioni> Comunicazionis { get; set; }

    public virtual DbSet<Corsi> Corsis { get; set; }

    public virtual DbSet<DispositiviAutorizzati> DispositiviAutorizzatis { get; set; }

    public virtual DbSet<Impostazioni> Impostazionis { get; set; }

    public virtual DbSet<Insegnanti> Insegnantis { get; set; }

    public virtual DbSet<Iscrizioni> Iscrizionis { get; set; }

    public virtual DbSet<Lezioni> Lezionis { get; set; }

    public virtual DbSet<PagamentiInsegnanti> PagamentiInsegnantis { get; set; }

    public virtual DbSet<Presenze> Presenzes { get; set; }

    public virtual DbSet<Privacy> Privacies { get; set; }

    public virtual DbSet<QuerySalvate> QuerySalvates { get; set; }

    public virtual DbSet<Sale> Sales { get; set; }

    public virtual DbSet<Stagioni> Stagionis { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Abbonamenti>(entity =>
        {
            entity.ToTable("Abbonamenti");

            entity.HasIndex(e => e.AllievoId, "IX_Abbonamenti_AllievoId");

            entity.HasIndex(e => e.CorsoId, "IX_Abbonamenti_CorsoId");

            entity.HasIndex(e => e.DataPagamento, "IX_Abbonamenti_DataPagamento");

            entity.Property(e => e.Attivo).HasDefaultValue(1);
            entity.Property(e => e.DataInizio).HasColumnType("DATETIME");
            entity.Property(e => e.DataPagamento).HasColumnType("DATETIME");
            entity.Property(e => e.DataScadenza).HasColumnType("DATETIME");
            entity.Property(e => e.DataSospensione).HasColumnType("DATETIME");
            entity.Property(e => e.DataStorno).HasColumnType("DATETIME");
            entity.Property(e => e.MetodoPagamento).HasDefaultValue("Contanti");
            entity.Property(e => e.Stornato).HasDefaultValue(0);
            entity.Property(e => e.TipoAbbonamento).HasDefaultValue("Mensile");

            entity.HasOne(d => d.Allievo).WithMany(p => p.Abbonamentis).HasForeignKey(d => d.AllievoId);

            entity.HasOne(d => d.Corso).WithMany(p => p.Abbonamentis).HasForeignKey(d => d.CorsoId);
        });

        modelBuilder.Entity<Allievi>(entity =>
        {
            entity.ToTable("Allievi");

            entity.Property(e => e.Attivo).HasDefaultValue(1);
            entity.Property(e => e.DaAbbonare).HasDefaultValue(0);
            entity.Property(e => e.DataRegistrazione).HasColumnType("DATETIME");
            entity.Property(e => e.NCivico).HasColumnName("N_Civico");
            entity.Property(e => e.Origine).HasDefaultValue("Segreteria");
        });

        modelBuilder.Entity<CalendarioChiusure>(entity =>
        {
            entity.ToTable("CalendarioChiusure");

            entity.HasIndex(e => e.StagioneId, "IX_CalendarioChiusure_StagioneId");

            entity.Property(e => e.DataFine).HasColumnName("Data_Fine");
            entity.Property(e => e.DataInizio).HasColumnName("Data_Inizio");
            entity.Property(e => e.EmailInviata).HasDefaultValue(0);
            entity.Property(e => e.Recupero).HasDefaultValue(1);
            entity.Property(e => e.Tipo).HasDefaultValue("Chiusura");

            entity.HasOne(d => d.Stagione).WithMany(p => p.CalendarioChiusures).HasForeignKey(d => d.StagioneId);
        });

        modelBuilder.Entity<CampiExtraAllievo>(entity =>
        {
            entity.ToTable("CampiExtraAllievo");

            entity.HasIndex(e => e.AllievoId, "IX_CampiExtraAllievo_AllievoId");

            entity.HasIndex(e => new { e.AllievoId, e.Chiave }, "IX_CampiExtraAllievo_Allievo_Chiave").IsUnique();

            entity.HasIndex(e => e.Chiave, "IX_CampiExtraAllievo_Chiave");

            entity.Property(e => e.Attivo).HasDefaultValue(1);
            entity.Property(e => e.DataInserimento).HasColumnType("DATETIME");
            entity.Property(e => e.Origine).HasDefaultValue("Tablet");

            entity.HasOne(d => d.Allievo).WithMany(p => p.CampiExtraAllievos)
                .HasForeignKey(d => d.AllievoId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Comunicazioni>(entity =>
        {
            entity.ToTable("Comunicazioni");

            entity.HasIndex(e => e.AllievoId, "IX_Comunicazioni_AllievoId");

            entity.HasIndex(e => e.DataInvio, "IX_Comunicazioni_DataInvio");

            entity.Property(e => e.Attivo).HasDefaultValue(1);
            entity.Property(e => e.DataInvio).HasColumnType("DATETIME");
            entity.Property(e => e.Esito).HasDefaultValue(1);
            entity.Property(e => e.Tipo).HasDefaultValue("Email");

            entity.HasOne(d => d.Allievo).WithMany(p => p.Comunicazionis).HasForeignKey(d => d.AllievoId);
        });

        modelBuilder.Entity<Corsi>(entity =>
        {
            entity.ToTable("Corsi");

            entity.Property(e => e.Attivo).HasDefaultValue(1);
            entity.Property(e => e.Colore).HasDefaultValue("#4F46E5");
            entity.Property(e => e.ColoreTesto).HasDefaultValue("#000000");
            entity.Property(e => e.CostoAnnuale).HasDefaultValue(0.0);
            entity.Property(e => e.CostoMensile).HasDefaultValue(0.0);
            entity.Property(e => e.CostoSingolo).HasDefaultValue(0.0);
        });

        modelBuilder.Entity<DispositiviAutorizzati>(entity =>
        {
            entity.ToTable("DispositiviAutorizzati");

            entity.HasIndex(e => e.Token, "IX_DispositiviAutorizzati_Token").IsUnique();

            entity.Property(e => e.Attivo).HasDefaultValue(1);
            entity.Property(e => e.UltimoAccesso).HasColumnType("DATETIME");
        });

        modelBuilder.Entity<Impostazioni>(entity =>
        {
            entity.ToTable("Impostazioni");

            entity.Property(e => e.ApiTabletAttiva).HasDefaultValue(0);
            entity.Property(e => e.ApiTabletPorta).HasDefaultValue(5000);
            entity.Property(e => e.ArchiviaRicevutePdf).HasDefaultValue(0);
            entity.Property(e => e.DispositivoPrivacy).HasDefaultValue(0);
            entity.Property(e => e.EmailUseSsl).HasDefaultValue(1);
            entity.Property(e => e.FunzioneMaestroAvanzataAttiva).HasDefaultValue(0);
            entity.Property(e => e.NumeroColoriCorsi).HasDefaultValue(6);
            entity.Property(e => e.OrarioScaglionato).HasDefaultValue(0);
            entity.Property(e => e.RicevutaAltezzaMm).HasDefaultValue(90);
            entity.Property(e => e.RicevutaLarghezzaMm).HasDefaultValue(90);
            entity.Property(e => e.RicevutaMarginBottomMm).HasDefaultValue(3);
            entity.Property(e => e.RicevutaMarginLeftMm).HasDefaultValue(5);
            entity.Property(e => e.RicevutaMarginRightMm).HasDefaultValue(3);
            entity.Property(e => e.RicevutaMarginTopMm).HasDefaultValue(10);
            entity.Property(e => e.StampaDocumentoPrivacy).HasDefaultValue(1);
            entity.Property(e => e.StampaRicevutaCortesia).HasDefaultValue(1);
            entity.Property(e => e.StampaSilenziosa).HasDefaultValue(0);

            entity.HasOne(d => d.StagioneCorrente).WithMany(p => p.Impostazionis).HasForeignKey(d => d.StagioneCorrenteId);
        });

        modelBuilder.Entity<Insegnanti>(entity =>
        {
            entity.ToTable("Insegnanti");

            entity.Property(e => e.Attivo).HasDefaultValue(1);
            entity.Property(e => e.TariffaOraria).HasDefaultValue(0.0);
        });

        modelBuilder.Entity<Iscrizioni>(entity =>
        {
            entity.ToTable("Iscrizioni");

            entity.Property(e => e.Attivo).HasDefaultValue(1);
            entity.Property(e => e.ImportoPagato).HasDefaultValue(0.0);
            entity.Property(e => e.ImportoTotale).HasDefaultValue(0.0);
            entity.Property(e => e.MesiRimanenti).HasDefaultValue(1);
            entity.Property(e => e.MesiTotali).HasDefaultValue(1);
            entity.Property(e => e.TipoAbbonamento).HasDefaultValue("Mensile");

            entity.HasOne(d => d.Allievo).WithMany(p => p.Iscrizionis)
                .HasForeignKey(d => d.AllievoId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Corso).WithMany(p => p.Iscrizionis)
                .HasForeignKey(d => d.CorsoId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Lezioni>(entity =>
        {
            entity.ToTable("Lezioni");

            entity.HasIndex(e => e.SalaId, "IX_Lezioni_SalaId");

            entity.HasOne(d => d.Corso).WithMany(p => p.Lezionis)
                .HasForeignKey(d => d.CorsoId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Insegnante).WithMany(p => p.Lezionis).HasForeignKey(d => d.InsegnanteId);

            entity.HasOne(d => d.Sala).WithMany(p => p.Lezionis).HasForeignKey(d => d.SalaId);
        });

        modelBuilder.Entity<PagamentiInsegnanti>(entity =>
        {
            entity.ToTable("PagamentiInsegnanti");

            entity.HasIndex(e => e.DataPagamento, "IX_PagamentiInsegnanti_DataPagamento");

            entity.HasIndex(e => e.InsegnanteId, "IX_PagamentiInsegnanti_InsegnanteId");

            entity.Property(e => e.Attivo).HasDefaultValue(1);
            entity.Property(e => e.DataPagamento).HasColumnType("DATETIME");
            entity.Property(e => e.MetodoPagamento).HasDefaultValue("Contanti");
            entity.Property(e => e.OreTotali).HasDefaultValue(0.0);
            entity.Property(e => e.PeriodoAl).HasColumnType("DATETIME");
            entity.Property(e => e.PeriodoDal).HasColumnType("DATETIME");

            entity.HasOne(d => d.Insegnante).WithMany(p => p.PagamentiInsegnantis)
                .HasForeignKey(d => d.InsegnanteId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Presenze>(entity =>
        {
            entity.ToTable("Presenze");

            entity.Property(e => e.Stato).HasDefaultValue(0);

            entity.HasOne(d => d.Allievo).WithMany(p => p.Presenzes)
                .HasForeignKey(d => d.AllievoId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Lezione).WithMany(p => p.Presenzes)
                .HasForeignKey(d => d.LezioneId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Privacy>(entity =>
        {
            entity.ToTable("Privacy");

            entity.HasIndex(e => e.IdAllievo, "IX_Privacy_Id_Allievo");

            entity.Property(e => e.Attivo).HasDefaultValue(1);
            entity.Property(e => e.DataFirma).HasColumnType("DATETIME");
            entity.Property(e => e.Firmato).HasDefaultValue(0);
            entity.Property(e => e.IdAllievo).HasColumnName("Id_Allievo");
            entity.Property(e => e.PresaVisione).HasDefaultValue(0);

            entity.HasOne(d => d.IdAllievoNavigation).WithMany(p => p.Privacies).HasForeignKey(d => d.IdAllievo);
        });

        modelBuilder.Entity<QuerySalvate>(entity =>
        {
            entity.ToTable("QuerySalvate");

            entity.Property(e => e.Attivo).HasDefaultValue(1);
            entity.Property(e => e.Categoria).HasDefaultValue("Generale");
            entity.Property(e => e.Ordine).HasDefaultValue(0);
            entity.Property(e => e.SoloAmministratore).HasDefaultValue(0);
            entity.Property(e => e.UltimaEsecuzione).HasColumnType("DATETIME");
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.ToTable("Sale");

            entity.Property(e => e.Attivo).HasDefaultValue(1);
            entity.Property(e => e.Capienza).HasDefaultValue(0);
            entity.Property(e => e.Colore).HasDefaultValue("#0EA5E9");
        });

        modelBuilder.Entity<Stagioni>(entity =>
        {
            entity.ToTable("Stagioni");

            entity.Property(e => e.Attivo).HasDefaultValue(1);
            entity.Property(e => e.DataFine).HasColumnType("DATETIME");
            entity.Property(e => e.DataInizio).HasColumnType("DATETIME");
            entity.Property(e => e.IsCorrente).HasDefaultValue(0);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
