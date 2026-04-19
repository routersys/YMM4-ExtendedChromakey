using ExtendedChromaKey.Localization;
using System.ComponentModel.DataAnnotations;

namespace ExtendedChromaKey.Models
{
    public enum AdvancedColorSpace
    {
        [Display(Name = nameof(Texts.AdvancedColorSpace_RGB), ResourceType = typeof(Texts))]
        RGB,
        [Display(Name = nameof(Texts.AdvancedColorSpace_HSV), ResourceType = typeof(Texts))]
        HSV,
        [Display(Name = nameof(Texts.AdvancedColorSpace_Lab), ResourceType = typeof(Texts))]
        Lab,
        [Display(Name = nameof(Texts.AdvancedColorSpace_YUV), ResourceType = typeof(Texts))]
        YUV,
        [Display(Name = nameof(Texts.AdvancedColorSpace_XYZ), ResourceType = typeof(Texts))]
        XYZ,
        [Display(Name = nameof(Texts.AdvancedColorSpace_LCH), ResourceType = typeof(Texts))]
        LCH,
        [Display(Name = nameof(Texts.AdvancedColorSpace_CIEDE2000), ResourceType = typeof(Texts))]
        CIEDE2000,
    }
}
