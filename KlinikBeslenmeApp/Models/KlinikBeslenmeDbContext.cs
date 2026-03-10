using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace KlinikBeslenmeApp.Models;

public partial class KlinikBeslenmeDbContext : DbContext
{
    public KlinikBeslenmeDbContext()
    {
    }

    public KlinikBeslenmeDbContext(DbContextOptions<KlinikBeslenmeDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<TblHastalar> TblHastalars { get; set; }

    public virtual DbSet<TblMalzemeler> TblMalzemelers { get; set; }
    public virtual DbSet<TblYemekler> TblYemeklers { get; set; }
    public virtual DbSet<TblYemekMalzemeleri> TblYemekMalzemeleris { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=ENES\\SQLEXPRESS02;Database=KlinikBeslenmeDB;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TblHastalar>(entity =>
        {
            entity.HasKey(e => e.HastaId).HasName("PK__tbl_Hast__114C5CAB482B4B1D");

            entity.ToTable("tbl_Hastalar");

            entity.HasIndex(e => e.Email, "UQ__tbl_Hast__A9D10534057FD1BF").IsUnique();

            entity.Property(e => e.HastaId).HasColumnName("HastaID");
            entity.Property(e => e.Ad).HasMaxLength(50);
            entity.Property(e => e.Cinsiyet).HasMaxLength(10);
            entity.Property(e => e.ColyakMi).HasDefaultValue(false);
            entity.Property(e => e.DiyabetMi).HasDefaultValue(false);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Ilce).HasMaxLength(50);
            entity.Property(e => e.Sehir).HasMaxLength(50);
            entity.Property(e => e.Sifre).HasMaxLength(50);
            entity.Property(e => e.Soyad).HasMaxLength(50);
            entity.Property(e => e.TansiyonHastasiMi).HasDefaultValue(false);
            entity.Property(e => e.Telefon).HasMaxLength(20);
        });

        modelBuilder.Entity<TblMalzemeler>(entity =>
        {
            entity.HasKey(e => e.MalzemeId).HasName("PK__tbl_Malz__4ED155E0A54CAD7C");

            entity.ToTable("tbl_Malzemeler");

            entity.Property(e => e.MalzemeId).HasColumnName("MalzemeID");
            entity.Property(e => e.Ad).HasMaxLength(100);
            entity.Property(e => e.Kategori).HasMaxLength(50);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
