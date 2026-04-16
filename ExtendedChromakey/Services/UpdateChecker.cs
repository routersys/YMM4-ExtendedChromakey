using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;

namespace ExtendedChromaKey.Services
{
    internal sealed class UpdateChecker : IDisposable
    {
        private const string ReleasesEndpoint = "https://api.github.com/repos/routersys/YMM4-ExtendedChromakey/releases/latest";
        private const string DefaultVersion = "0.0.0";

        private readonly HttpClient _httpClient;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private string? _updateMessage;
        private bool _checked;
        private int _disposed;

        public UpdateChecker()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10),
            };
            _httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("YMM4-ExtendedChromakey", GetCurrentVersion()));
        }

        public string? UpdateMessage => Volatile.Read(ref _updateMessage);

        public async Task CheckAsync(CancellationToken cancellationToken = default)
        {
            if (Volatile.Read(ref _disposed) == 1)
                return;
            if (Volatile.Read(ref _checked))
                return;

            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_checked) return;

                using var response = await _httpClient.GetAsync(ReleasesEndpoint, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                using var jsonDoc = JsonDocument.Parse(jsonString);

                if (jsonDoc.RootElement.TryGetProperty("tag_name", out var tagNameElement))
                {
                    var latestVersionTag = tagNameElement.GetString() ?? string.Empty;
                    var latestVersionStr = latestVersionTag.StartsWith('v') ? latestVersionTag[1..] : latestVersionTag;

                    if (Version.TryParse(latestVersionStr, out var latestVersion) &&
                        Version.TryParse(GetCurrentVersion(), out var currentVersion) &&
                        latestVersion > currentVersion)
                    {
                        Volatile.Write(ref _updateMessage,
                            $"新しいバージョン v{latestVersionStr} が利用可能です。(現在: v{currentVersion})");
                    }
                }
            }
            catch
            {
            }
            finally
            {
                Volatile.Write(ref _checked, true);
                _semaphore.Release();
            }
        }

        public static string GetCurrentVersion() =>
            Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? DefaultVersion;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
                return;

            _semaphore.Dispose();
            _httpClient.Dispose();
        }
    }
}