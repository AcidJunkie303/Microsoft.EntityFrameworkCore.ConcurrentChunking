using Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Logging;
using Xunit;

namespace ConcurrentChunking.Testing;

public abstract class TestBase : IDisposable
{
    protected XunitLoggerFactory LoggerFactory { get; }
    public ITestOutputHelper TestOutputHelper { get; }

    protected TestBase(ITestOutputHelper testOutputHelper)
    {
        TestOutputHelper = testOutputHelper;
        LoggerFactory = new XunitLoggerFactory(testOutputHelper);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            LoggerFactory.Dispose();
        }
    }
}
