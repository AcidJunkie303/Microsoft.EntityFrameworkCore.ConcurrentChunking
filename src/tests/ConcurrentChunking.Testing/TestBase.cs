using Microsoft.EntityFrameworkCore.ConcurrentChunking;
using Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Logging;
using Xunit;

namespace ConcurrentChunking.Testing;

public abstract class TestBase : IDisposable
{
    protected XunitLoggerFactory LoggerFactory { get; }

    protected TestBase(ITestOutputHelper testOutputHelper)
    {
        LoggerFactory = new XunitLoggerFactory(testOutputHelper);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected static bool IsChunkOrderSequential<T>(in IReadOnlyList<Chunk<T>> chunks)
        => !chunks.Where((chunk, i) => chunk.ChunkIndex != i).Any();

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            LoggerFactory.Dispose();
        }
    }
}
