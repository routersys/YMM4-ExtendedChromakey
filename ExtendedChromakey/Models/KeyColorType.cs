using ExtendedChromaKey.Localization;
using System.ComponentModel.DataAnnotations;

namespace ExtendedChromaKey.Models
{
    public enum KeyColorType
    {
        [Display(Name = nameof(Texts.KeyColorType_Custom), ResourceType = typeof(Texts))]
        Custom,
        [Display(Name = nameof(Texts.KeyColorType_Green), ResourceType = typeof(Texts))]
        Green,
        [Display(Name = nameof(Texts.KeyColorType_Blue), ResourceType = typeof(Texts))]
        Blue,
        [Display(Name = nameof(Texts.KeyColorType_Red), ResourceType = typeof(Texts))]
        Red,
    }
}
