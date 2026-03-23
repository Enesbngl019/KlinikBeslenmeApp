using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KlinikBeslenmeApp.Models
{
    [Table("TblKiloGecmisi")] 
    public class TblKiloGecmisi
    {
        [Key]
        public int KiloGecmisId { get; set; }
        public int HastaId { get; set; }
        public double Kilo { get; set; }
        public DateTime TartilmaTarihi { get; set; }
    }
}