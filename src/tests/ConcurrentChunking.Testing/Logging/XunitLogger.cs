using System.Text;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Logging;

public sealed class XunitLogger<T> : XunitLogger, ILogger<T>
{
    private static readonly string TypeName = GetTypeName();

    public XunitLogger(ITestOutputHelper testOutputHelper, LoggerExternalScopeProvider scopeProvider)
        : base(testOutputHelper, scopeProvider, TypeName)
    {
    }

    private static string GetTypeName()
    {
        var type = typeof(T);
        return type.Namespace is null
            ? type.Name
            : $"{type.Namespace}.{type.Name}";
    }
}

public class XunitLogger : ILogger
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

        var message = $"{DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss.fff'Z'} {GetLogLevelString(logLevel)} [{_categoryName}] {formatter(state, exception)}";
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
