using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KlinikBeslenmeApp.Models
{
    [Table("TblSuGunlugu")]
    public class TblSuGunlugu
    {
        [Key]
        public int SuId { get; set; }
        public int HastaId { get; set; }
        public int MiktarMililitre { get; set; }
        public DateTime Tarih { get; set; }
    }
}