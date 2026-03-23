using System;
using System.Collections.Generic;

namespace KlinikBeslenmeApp.Models;

public partial class TblHastalar
{
    public int HastaId { get; set; }

    public string Sifre { get; set; } = null!;

    public string? Telefon { get; set; }

    public string? Sehir { get; set; }

    public string? Ilce { get; set; }

    public string? Adres { get; set; }

    public string? Cinsiyet { get; set; }

    public int? Boy { get; set; }

    public double? Kilo { get; set; }

    public bool? ColyakMi { get; set; }

    public bool? DiyabetMi { get; set; }

    public bool? TansiyonHastasiMi { get; set; }

    public string Ad { get; set; } = null!;

    public string Soyad { get; set; } = null!;
    public string Email { get; set; } = null!;
    public DateTime DogumTarihi { get; set; }
    public int Yas { get; set; }
    public int? DoktorId { get; set; }
    public int? DoktorOnayDurumu { get; set; }
}
