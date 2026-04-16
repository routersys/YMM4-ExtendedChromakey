using ExtendedChromaKey.Effect;

namespace ExtendedChromaKey.Models
{
    public readonly record struct ValidationRule(
        string Name,
        Func<ExtendedChromaKeyEffect, bool> Check,
        string ErrorMessage,
        ValidationLevel Level);
}