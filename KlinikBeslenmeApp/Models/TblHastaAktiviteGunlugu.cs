using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KlinikBeslenmeApp.Models
{
    [Table("TblHastaAktiviteGunlugu")]
    public class TblHastaAktiviteGunlugu
    {
        [Key]
        public int GunlukId { get; set; }
        public int HastaId { get; set; }
        public int AktiviteId { get; set; }
        public int SureDakika { get; set; }
        public double YakilanKalori { get; set; }
        public DateTime Tarih { get; set; }
    }
}