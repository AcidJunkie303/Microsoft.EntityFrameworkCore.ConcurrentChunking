using Xunit.Abstractions;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.Tests;

public sealed partial class OrderedQueryableExtensionsTests : IDisposable
{
    private readonly XUnitLoggerFactory _loggerFactory;

    static OrderedQueryableExtensionsTests()
    {
        TestData.EnsureInitialized();
    }

    public OrderedQueryableExtensionsTests(ITestOutputHelper testOutputHelper)
    {
        _loggerFactory = new XUnitLoggerFactory(testOutputHelper);
    }

    public void Dispose() => _loggerFactory.Dispose();

    private static bool IsChunkOrderSequential<T>(in List<Chunk<T>> chunks)
        => !chunks.Where((chunk, i) => chunk.ChunkIndex != i).Any();
}
