using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KlinikBeslenmeApp.Models
{
    [Table("tbl_YemekGunlugu")]
    public class TblYemekGunlugu
    {
        [Key]
        public int GunlukId { get; set; }
        public int HastaId { get; set; }
        public int YemekId { get; set; }
        public string OgunTipi { get; set; }
        public DateTime TuketimTarihi { get; set; }
        public string Aciklama { get; set; }

        public double Porsiyon { get; set; } = 1.0;
        public string? KayitId { get; set; }
    }
}