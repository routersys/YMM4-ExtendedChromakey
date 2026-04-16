using System.ComponentModel.DataAnnotations;

namespace ExtendedChromaKey.Models
{
    public enum AdvancedColorSpace
    {
        [Display(Name = "RGB(標準)")]
        RGB,
        [Display(Name = "HSV(色相重視)")]
        HSV,
        [Display(Name = "CIE Lab(知覚的)")]
        Lab,
        [Display(Name = "YUV(放送品質)")]
        YUV,
        [Display(Name = "XYZ(CIE標準)")]
        XYZ,
        [Display(Name = "LCH(知覚円筒)")]
        LCH,
        [Display(Name = "CIEDE2000(最高精度)")]
        CIEDE2000,
    }
}