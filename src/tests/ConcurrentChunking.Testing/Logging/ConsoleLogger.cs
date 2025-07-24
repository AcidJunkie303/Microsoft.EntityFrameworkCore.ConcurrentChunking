using Microsoft.Extensions.Logging;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Logging;

public class ConsoleLogger : ILogger
{
    private readonly string _categoryName;

    public ConsoleLogger(string categoryName)
    {
        _categoryName = categoryName;
    }

    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull
        => new NullDisposable();

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

        Console.WriteLine(message);
    }

    public bool IsEnabled(LogLevel logLevel) => true;

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

    private sealed class NullDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}

public sealed class ConsoleLogger<T> : ConsoleLogger, ILogger<T>
{
    public ConsoleLogger() : base(typeof(T).Name)
    {
    }
}
