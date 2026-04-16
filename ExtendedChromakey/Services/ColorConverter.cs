using System.Numerics;
using System.Runtime.CompilerServices;
using WpfColor = System.Windows.Media.Color;

namespace ExtendedChromaKey.Services
{
    internal static class ColorConverter
    {
        private const float Inv255 = 1.0f / 255.0f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 ToVector4(WpfColor color) =>
            new(color.R * Inv255, color.G * Inv255, color.B * Inv255, color.A * Inv255);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToVector3(WpfColor color) =>
            new(color.R * Inv255, color.G * Inv255, color.B * Inv255);
    }
}