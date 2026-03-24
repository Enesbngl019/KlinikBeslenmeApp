using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KlinikBeslenmeApp.Models
{
    [Table("TblAktiviteler")]
    public class TblAktiviteler
    {
        [Key]
        public int AktiviteId { get; set; }
        public string AktiviteAdi { get; set; }
        public double MetDegeri { get; set; }
    }
}