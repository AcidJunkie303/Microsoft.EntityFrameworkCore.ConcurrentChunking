using System.Text;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.IntegrationTests.Support;

internal sealed class XunitLoggerFactory : ILoggerFactory
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

    public ILogger<T> CreateLogger<T>() => new XunitLogger<T>(_outputHelper, _scopeProvider, typeof(T).Name);

    public ILogger<T> CreateLogger<T>(T obj)
    {
        ArgumentNullException.ThrowIfNull(obj);
        return new XunitLogger<T>(_outputHelper, _scopeProvider, obj.GetType().Name);
    }

    public void AddProvider(ILoggerProvider provider)
    {
        // No-op: this factory only supports its own logger provider.
    }

    public void Dispose() => _disposed = true;

    private class XunitLogger : ILogger
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly LoggerExternalScopeProvider _scopeProvider;
        private readonly string _categoryName;

        public XunitLogger(ITestOutputHelper testOutputHelper, LoggerExternalScopeProvider scopeProvider, string categoryName)
        {
            _testOutputHelper = testOutputHelper;
            _scopeProvider = scopeProvider;
            _categoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
            => _scopeProvider.Push(state);

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var message = $"{GetLogLevelString(logLevel)} [{_categoryName}] {formatter(state, exception)}";
            if (exception != null)
            {
                message += Environment.NewLine + exception;
            }

            // Append scopes if any
#pragma warning disable CA1305
            _scopeProvider.ForEachScope((scope, sb) => sb.Append($"\n => {scope}"), new StringBuilder(message));
#pragma warning restore CA1305

            _testOutputHelper.WriteLine(message);
        }

        private static string GetLogLevelString(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Trace       => "trce",
                LogLevel.Debug       => "dbug",
                LogLevel.Information => "info",
                LogLevel.Warning     => "warn",
                LogLevel.Error       => "fail",
                LogLevel.Critical    => "crit",
                LogLevel.None        => "none",
                _                    => throw new ArgumentOutOfRangeException(nameof(logLevel))
            };
        }
    }

    private sealed class XunitLogger<T> : XunitLogger, ILogger<T>
    {
        public XunitLogger(ITestOutputHelper testOutputHelper, LoggerExternalScopeProvider scopeProvider, string categoryName)
            : base(testOutputHelper, scopeProvider, categoryName)
        {
        }
    }
}
