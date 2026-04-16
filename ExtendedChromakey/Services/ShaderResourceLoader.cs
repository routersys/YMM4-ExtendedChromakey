using System.Reflection;

namespace ExtendedChromaKey.Services
{
    internal static class ShaderResourceLoader
    {
        private const string ShaderSuffix = "ExtendedChromaKeyPS.cso";

        private static readonly Lazy<byte[]> _shader =
            new(LoadShader, LazyThreadSafetyMode.ExecutionAndPublication);

        public static byte[] GetExtendedChromaKeyPS() => _shader.Value;

        private static byte[] LoadShader()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var names = assembly.GetManifestResourceNames();

            string? matched = null;
            foreach (var name in names)
            {
                if (name.EndsWith(ShaderSuffix, StringComparison.Ordinal))
                {
                    matched = name;
                    break;
                }
            }

            if (matched is null)
                throw new InvalidOperationException(
                    $"Embedded resource '{ShaderSuffix}' was not found in the assembly manifest.");

            using var stream = assembly.GetManifestResourceStream(matched)!;
            var buffer = new byte[(int)stream.Length];
            stream.ReadExactly(buffer);
            return buffer;
        }
    }
}