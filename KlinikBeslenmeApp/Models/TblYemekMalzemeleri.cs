using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KlinikBeslenmeApp.Models
{
    [Table("tbl_YemekMalzemeleri")] 
    public class TblYemekMalzemeleri
    {
        [Key]
        public int ReceteId { get; set; }
        public int YemekId { get; set; }
        public int MalzemeId { get; set; }

    
        public decimal MiktarGram { get; set; }
    }
}