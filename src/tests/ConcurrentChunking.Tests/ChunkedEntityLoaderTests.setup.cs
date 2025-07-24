using Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing;
using Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Entities;
using Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Logging;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.Tests;

public sealed partial class ChunkedEntityLoaderTests : IDisposable
{
    private static readonly int EntityCount = InMemoryTestData.EntityCount;
    private readonly XunitLoggerFactory _loggerFactory;

    public ChunkedEntityLoaderTests(ITestOutputHelper testOutputHelper)
    {
        _loggerFactory = new XunitLoggerFactory(testOutputHelper);
    }

    public void Dispose() => _loggerFactory.Dispose();

    private static bool IsChunkOrderSequential<T>(in List<Chunk<T>> chunks)
        => !chunks.Where((chunk, i) => chunk.ChunkIndex != i).Any();

    private ChunkedEntityLoader<InMemoryDbContext, SimpleEntity> CreateLoader
    (
        int chunkSize,
        int maxConcurrentProducerCount,
        int maxPrefetchCount,
        ChunkedEntityLoaderOptions options
    )
    {
        return new ChunkedEntityLoader<InMemoryDbContext, SimpleEntity>(
            dbContextFactory: () => new InMemoryDbContext(),
            chunkSize: chunkSize,
            maxConcurrentProducerCount: maxConcurrentProducerCount,
            maxPrefetchCount: maxPrefetchCount,
            sourceQueryProvider: ctx => ctx.SimpleEntities.AsNoTracking().OrderBy(e => e.Id),
            options: options,
            loggerFactory: _loggerFactory,
            logger: _loggerFactory.CreateLogger<ChunkedEntityLoader<InMemoryDbContext, SimpleEntity>>()
        );
    }
}
