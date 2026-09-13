using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Pulse.Models;

public partial class PulseContext : DbContext
{
    public PulseContext()
    {
    }

    public PulseContext(DbContextOptions<PulseContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Abbonamenti> Abbonamentis { get; set; }

    public virtual DbSet<Allievi> Allievis { get; set; }

    public virtual DbSet<CalendarioChiusure> CalendarioChiusures { get; set; }

    public virtual DbSet<Corsi> Corsis { get; set; }

    public virtual DbSet<Impostazioni> Impostazionis { get; set; }

    public virtual DbSet<Insegnanti> Insegnantis { get; set; }

    public virtual DbSet<Iscrizioni> Iscrizionis { get; set; }

    public virtual DbSet<Lezioni> Lezionis { get; set; }

    public virtual DbSet<Presenze> Presenzes { get; set; }

    public virtual DbSet<Privacy> Privacies { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Abbonamenti>(entity =>
        {
            entity.ToTable("Abbonamenti");

            entity.HasIndex(e => e.AllievoId, "IX_Abbonamenti_AllievoId");

            entity.HasIndex(e => e.CorsoId, "IX_Abbonamenti_CorsoId");

            entity.Property(e => e.Attivo).HasDefaultValue(1);
            entity.Property(e => e.TipoAbbonamento).HasDefaultValue("Mensile");

            entity.HasOne(d => d.Allievo).WithMany(p => p.Abbonamentis).HasForeignKey(d => d.AllievoId);

            entity.HasOne(d => d.Corso).WithMany(p => p.Abbonamentis).HasForeignKey(d => d.CorsoId);
        });

        modelBuilder.Entity<Allievi>(entity =>
        {
            entity.ToTable("Allievi");

            entity.Property(e => e.Attivo).HasDefaultValue(1);
            entity.Property(e => e.NCivico).HasColumnName("N_Civico");
        });

        modelBuilder.Entity<CalendarioChiusure>(entity =>
        {
            entity.ToTable("CalendarioChiusure");

            entity.Property(e => e.DataFine).HasColumnName("Data_Fine");
            entity.Property(e => e.DataInizio).HasColumnName("Data_Inizio");
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

        modelBuilder.Entity<Impostazioni>(entity =>
        {
            entity.ToTable("Impostazioni");

            entity.Property(e => e.EmailUseSsl).HasDefaultValue(1);
            entity.Property(e => e.NumeroColoriCorsi).HasDefaultValue(6);
            entity.Property(e => e.OrarioScaglionato).HasDefaultValue(0);
            entity.Property(e => e.StampaDocumentoPrivacy).HasDefaultValue(1);
            entity.Property(e => e.StampaRicevutaCortesia).HasDefaultValue(1);
        });

        modelBuilder.Entity<Insegnanti>(entity =>
        {
            entity.ToTable("Insegnanti");

            entity.Property(e => e.Attivo).HasDefaultValue(1);
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

            entity.HasOne(d => d.Corso).WithMany(p => p.Lezionis)
                .HasForeignKey(d => d.CorsoId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Insegnante).WithMany(p => p.Lezionis).HasForeignKey(d => d.InsegnanteId);
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

            entity.Property(e => e.IdAllievo).HasColumnName("Id_Allievo");

            entity.HasOne(d => d.IdAllievoNavigation).WithMany(p => p.Privacies).HasForeignKey(d => d.IdAllievo);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
