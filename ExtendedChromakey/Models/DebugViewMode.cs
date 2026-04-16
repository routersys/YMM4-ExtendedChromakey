using System.ComponentModel.DataAnnotations;

namespace ExtendedChromaKey.Models
{
    public enum DebugViewMode
    {
        [Display(Name = "最終結果")]
        Result,
        [Display(Name = "マスク表示")]
        Matte,
        [Display(Name = "色距離")]
        ColorDistance,
        [Display(Name = "スピル除去後")]
        SpillSuppressed,
        [Display(Name = "色補正後")]
        ColorCorrected,
    }
}