using Xunit.Abstractions;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.Tests;

public sealed partial class ChunkedEntityLoaderTests : IDisposable
{
    private const int EntityCount = 10001;
    private readonly XUnitLoggerFactory _loggerFactory;

    static ChunkedEntityLoaderTests()
    {
        TestData.EnsureInitialized();
    }

    public ChunkedEntityLoaderTests(ITestOutputHelper testOutputHelper)
    {
        _loggerFactory = new XUnitLoggerFactory(testOutputHelper);
    }

    public void Dispose() => _loggerFactory.Dispose();

    private static bool IsChunkOrderSequential<T>(in List<Chunk<T>> chunks)
        => !chunks.Where((chunk, i) => chunk.ChunkIndex != i).Any();

    private ChunkedEntityLoader<MyDbContext, SimpleEntity> CreateLoader
    (
        int chunkSize,
        int maxConcurrentProducerCount,
        ChunkedEntityLoaderOptions options
    )
    {
        return new ChunkedEntityLoader<MyDbContext, SimpleEntity>(
            dbContextFactory: new MyDbContextFactory(),
            chunkSize: chunkSize,
            maxConcurrentProducerCount: maxConcurrentProducerCount,
            sourceQueryProvider: ctx => ctx.SimpleEntities.OrderBy(e => e.Id),
            options: options,
            loggerFactory: _loggerFactory,
            logger: _loggerFactory.CreateLogger<ChunkedEntityLoader<MyDbContext, SimpleEntity>>()
        );
    }
}
