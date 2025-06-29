using System.Text;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.IntegrationTests;

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

    public void AddProvider(ILoggerProvider provider)
    {
        // No-op: this factory only supports its own logger provider.
    }

    public void Dispose() => _disposed = true;

    private sealed class XunitLogger : ILogger
    {
        private readonly ITestOutputHelper _outputHelper;
        private readonly LoggerExternalScopeProvider _scopeProvider;
        private readonly string _categoryName;

        public XunitLogger(ITestOutputHelper outputHelper, LoggerExternalScopeProvider scopeProvider, string categoryName)
        {
            _outputHelper = outputHelper;
            _scopeProvider = scopeProvider;
            _categoryName = categoryName;
        }

        public IDisposable? BeginScope<TState>(TState state)
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

            _outputHelper.WriteLine(message);
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
                _                    => throw new ArgumentOutOfRangeException(nameof(logLevel))
            };
        }
    }
}
