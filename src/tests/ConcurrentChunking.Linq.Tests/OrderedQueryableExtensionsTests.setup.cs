using Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing;
using Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Logging;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.Linq.Tests;

public sealed partial class OrderedQueryableExtensionsTests : IDisposable
{
    private static readonly int EntityCount = InMemoryTestData.EntityCount;
    private readonly XunitLoggerFactory _loggerFactory;

    public OrderedQueryableExtensionsTests(ITestOutputHelper testOutputHelper)
    {
        _loggerFactory = new XunitLoggerFactory(testOutputHelper);
    }

    public void Dispose() => _loggerFactory.Dispose();

    private static bool IsChunkOrderSequential<T>(in List<Chunk<T>> chunks)
        => !chunks.Where((chunk, i) => chunk.ChunkIndex != i).Any();
}
