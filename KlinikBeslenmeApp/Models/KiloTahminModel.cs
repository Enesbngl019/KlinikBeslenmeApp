using Microsoft.ML.Data;

namespace KlinikBeslenmeApp.Models
{
    public class KiloVerisi
    {
        [LoadColumn(0)]
        public float GecenGunSayisi { get; set; }

        [LoadColumn(1)]
        public float Kilo { get; set; }
    }

    public class KiloTahmini
    {
        [ColumnName("Score")]
        public float BeklenenKilo { get; set; }
    }
}