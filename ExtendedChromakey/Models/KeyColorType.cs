using System.ComponentModel.DataAnnotations;

namespace ExtendedChromaKey.Models
{
    public enum KeyColorType
    {
        [Display(Name = "カスタム")]
        Custom,
        [Display(Name = "緑")]
        Green,
        [Display(Name = "青")]
        Blue,
        [Display(Name = "赤")]
        Red,
    }
}