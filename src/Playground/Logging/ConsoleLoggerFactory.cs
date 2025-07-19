using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Playground.Logging;

[SuppressMessage("Performance", "CA1822:Mark members as static")]
[SuppressMessage("Minor Code Smell", "S2325:Methods and properties that don\'t access instance data should be static")]
public sealed class ConsoleLoggerFactory : ILoggerFactory
{
    public void Dispose()
    {
    }

    public ILogger CreateLogger(string categoryName) => new ConsoleLogger(categoryName);
    public ILogger<T> CreateLogger<T>() => new ConsoleLogger<T>();

    public void AddProvider(ILoggerProvider provider)
    {
    }
}
