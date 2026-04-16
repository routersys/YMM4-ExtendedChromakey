using System.ComponentModel.DataAnnotations;

namespace ExtendedChromaKey.Models
{
    public enum QualityPreset
    {
        [Display(Name = "高速")]
        Fast,
        [Display(Name = "バランス")]
        Balanced,
        [Display(Name = "高品質")]
        HighQuality,
    }
}