using ExtendedChromaKey.Localization;
using System.ComponentModel.DataAnnotations;

namespace ExtendedChromaKey.Models
{
    public enum QualityPreset
    {
        [Display(Name = nameof(Texts.QualityPreset_Fast), ResourceType = typeof(Texts))]
        Fast,
        [Display(Name = nameof(Texts.QualityPreset_Balanced), ResourceType = typeof(Texts))]
        Balanced,
        [Display(Name = nameof(Texts.QualityPreset_HighQuality), ResourceType = typeof(Texts))]
        HighQuality,
    }
}
