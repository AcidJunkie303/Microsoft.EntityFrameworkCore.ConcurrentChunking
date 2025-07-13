using Microsoft.EntityFrameworkCore.ConcurrentChunking.Tests.Entities;
using Microsoft.EntityFrameworkCore.ConcurrentChunking.Tests.Support;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.Tests;

public sealed partial class ChunkedEntityLoaderTests : IDisposable
{
    private const int EntityCount = 10001;
    private readonly XunitLoggerFactory _loggerFactory;

    public ChunkedEntityLoaderTests(ITestOutputHelper testOutputHelper)
    {
        _loggerFactory = new XunitLoggerFactory(testOutputHelper);
    }

    public void Dispose() => _loggerFactory.Dispose();

    private static bool IsChunkOrderSequential<T>(in List<Chunk<T>> chunks)
        => !chunks.Where((chunk, i) => chunk.ChunkIndex != i).Any();

    private ChunkedEntityLoader<TestDbContext, SimpleEntity> CreateLoader
    (
        int chunkSize,
        int maxConcurrentProducerCount,
        int maxPrefetchCount,
        ChunkedEntityLoaderOptions options
    )
    {
        return new ChunkedEntityLoader<TestDbContext, SimpleEntity>(
            dbContextFactory: new TestDbContextFactory(),
            chunkSize: chunkSize,
            maxConcurrentProducerCount: maxConcurrentProducerCount,
            maxPrefetchCount: maxPrefetchCount,
            sourceQueryProvider: ctx => ctx.SimpleEntities.OrderBy(e => e.Id),
            options: options,
            loggerFactory: _loggerFactory,
            logger: _loggerFactory.CreateLogger<ChunkedEntityLoader<TestDbContext, SimpleEntity>>()
        );
    }
}
