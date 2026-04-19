using ExtendedChromaKey.Localization;
using System.ComponentModel.DataAnnotations;

namespace ExtendedChromaKey.Models
{
    public enum DebugViewMode
    {
        [Display(Name = nameof(Texts.DebugViewMode_Result), ResourceType = typeof(Texts))]
        Result,
        [Display(Name = nameof(Texts.DebugViewMode_Matte), ResourceType = typeof(Texts))]
        Matte,
        [Display(Name = nameof(Texts.DebugViewMode_ColorDistance), ResourceType = typeof(Texts))]
        ColorDistance,
        [Display(Name = nameof(Texts.DebugViewMode_SpillSuppressed), ResourceType = typeof(Texts))]
        SpillSuppressed,
        [Display(Name = nameof(Texts.DebugViewMode_ColorCorrected), ResourceType = typeof(Texts))]
        ColorCorrected,
    }
}
