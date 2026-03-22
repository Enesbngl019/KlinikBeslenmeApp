using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; 

namespace KlinikBeslenmeApp.Models
{
    [Table("TblDoktorlar")] 
    public class TblDoktorlar
    {
        [Key]
        public int DoktorId { get; set; }
        public string AdSoyad { get; set; }
        public string Email { get; set; }
        public string Sifre { get; set; }
        public bool IlkGirisMi { get; set; }
    }
}