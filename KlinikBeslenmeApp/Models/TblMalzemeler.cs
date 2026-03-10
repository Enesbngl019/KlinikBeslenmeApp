using System;
using System.Collections.Generic;

namespace KlinikBeslenmeApp.Models;

public partial class TblMalzemeler
{
    public int MalzemeId { get; set; }

    public string? Ad { get; set; }

    public string? Kategori { get; set; }

    public double? Kalori { get; set; }

    public double? Protein { get; set; }

    public double? Yag { get; set; }

    public double? Karbonhidrat { get; set; }

    public double? Sodyum { get; set; }

    public int? GlisemikIndeks { get; set; }

    public bool? GlutenVarMi { get; set; }
}
