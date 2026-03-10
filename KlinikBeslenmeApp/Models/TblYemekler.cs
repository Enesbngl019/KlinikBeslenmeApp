using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KlinikBeslenmeApp.Models
{
    [Table("tbl_Yemekler")] 
    public class TblYemekler
    {
        [Key]
        public int YemekId { get; set; }
        public string YemekAdi { get; set; }
        public string Kategori { get; set; }
    }
}