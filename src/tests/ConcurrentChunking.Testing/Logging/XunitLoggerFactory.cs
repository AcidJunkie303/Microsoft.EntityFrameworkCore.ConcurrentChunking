using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Logging;

public sealed class XunitLoggerFactory : ILoggerFactory
{
    private readonly ITestOutputHelper _outputHelper;
    private readonly LoggerExternalScopeProvider _scopeProvider = new();
    private bool _disposed;

    public XunitLoggerFactory(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
    }

    public ILogger CreateLogger(string categoryName)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return new XunitLogger(_outputHelper, _scopeProvider, categoryName);
    }

    public ILogger<T> CreateLogger<T>() => new XunitLogger<T>(_outputHelper, _scopeProvider);

    public ILogger<T> CreateLogger<T>(T obj)
    {
        ArgumentNullException.ThrowIfNull(obj);
        return new XunitLogger<T>(_outputHelper, _scopeProvider);
    }

    public void AddProvider(ILoggerProvider provider)
    {
        // No-op: this factory only supports its own logger provider.
    }

    public void Dispose() => _disposed = true;
}
