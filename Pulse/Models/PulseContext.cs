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

    public virtual DbSet<Allievi> Allievis { get; set; }

    public virtual DbSet<Corsi> Corsis { get; set; }

    public virtual DbSet<Insegnanti> Insegnantis { get; set; }

    public virtual DbSet<Iscrizioni> Iscrizionis { get; set; }

    public virtual DbSet<Lezioni> Lezionis { get; set; }

    public virtual DbSet<Presenze> Presenzes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Allievi>(entity =>
        {
            entity.ToTable("Allievi");

            entity.Property(e => e.Attivo).HasDefaultValue(1);
            entity.Property(e => e.DataIscrizione).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<Corsi>(entity =>
        {
            entity.ToTable("Corsi");

            entity.Property(e => e.Attivo).HasDefaultValue(1);
            entity.Property(e => e.Colore).HasDefaultValue("#4F46E5");
        });

        modelBuilder.Entity<Insegnanti>(entity =>
        {
            entity.ToTable("Insegnanti");

            entity.Property(e => e.Attivo).HasDefaultValue(1);
        });

        modelBuilder.Entity<Iscrizioni>(entity =>
        {
            entity.ToTable("Iscrizioni");

            entity.HasIndex(e => new { e.AllievoId, e.CorsoId }, "IX_Iscrizioni_AllievoId_CorsoId").IsUnique();

            entity.Property(e => e.Attivo).HasDefaultValue(1);
            entity.Property(e => e.DataIscrizione).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Allievo).WithMany(p => p.Iscrizionis).HasForeignKey(d => d.AllievoId);

            entity.HasOne(d => d.Corso).WithMany(p => p.Iscrizionis).HasForeignKey(d => d.CorsoId);
        });

        modelBuilder.Entity<Lezioni>(entity =>
        {
            entity.ToTable("Lezioni");

            entity.HasOne(d => d.Corso).WithMany(p => p.Lezionis).HasForeignKey(d => d.CorsoId);

            entity.HasOne(d => d.Insegnante).WithMany(p => p.Lezionis).HasForeignKey(d => d.InsegnanteId);
        });

        modelBuilder.Entity<Presenze>(entity =>
        {
            entity.ToTable("Presenze");

            entity.Property(e => e.Data).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Allievo).WithMany(p => p.Presenzes).HasForeignKey(d => d.AllievoId);

            entity.HasOne(d => d.Lezione).WithMany(p => p.Presenzes).HasForeignKey(d => d.LezioneId);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
