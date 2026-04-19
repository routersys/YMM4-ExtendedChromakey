using ExtendedChromaKey.Effect;
using YukkuriMovieMaker.Commons;

namespace ExtendedChromaKey.Services
{
    internal sealed class ServiceRegistry : IDisposable
    {
        private static readonly Lazy<ServiceRegistry> _instance =
            new(static () => new ServiceRegistry(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static ServiceRegistry Instance => _instance.Value;

        private readonly GraphicsEffectPool _effectPool = new();
        private readonly UpdateChecker _updateChecker = new();
        private readonly ValidationRuleProvider _validationRuleProvider = new();
        private int _disposed;

        private ServiceRegistry()
        {
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        }

        private static void OnProcessExit(object? sender, EventArgs e)
        {
            if (_instance.IsValueCreated)
                _instance.Value.Dispose();
        }

        public GraphicsEffectPool EffectPool
        {
            get
            {
                ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) == 1, this);
                return _effectPool;
            }
        }

        public UpdateChecker UpdateChecker
        {
            get
            {
                ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) == 1, this);
                return _updateChecker;
            }
        }

        public ValidationRuleProvider ValidationRuleProvider
        {
            get
            {
                ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) == 1, this);
                return _validationRuleProvider;
            }
        }

        public ExtendedChromaKeyEffectProcessor CreateProcessor(
            IGraphicsDevicesAndContext devices,
            ExtendedChromaKeyEffect effect)
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) == 1, this);
            return new ExtendedChromaKeyEffectProcessor(devices, effect);
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
                return;

            AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
            _effectPool.Dispose();
            _updateChecker.Dispose();
        }
    }
}
