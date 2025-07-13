using Microsoft.EntityFrameworkCore.ConcurrentChunking.IntegrationTests.Entities;
using Microsoft.EntityFrameworkCore.ConcurrentChunking.IntegrationTests.Support;
using Xunit;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.IntegrationTests;

public sealed partial class ChunkedEntityLoaderTests : IDisposable
{
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
        ChunkedEntityLoaderOptions options
    )
    {
        return new ChunkedEntityLoader<TestDbContext, SimpleEntity>(
            dbContextFactory: new TestDbContextFactory(),
            chunkSize: chunkSize,
            maxConcurrentProducerCount: maxConcurrentProducerCount,
            sourceQueryProvider: ctx => ctx.SimpleEntities.OrderBy(e => e.Id),
            options: options,
            loggerFactory: _loggerFactory,
            logger: _loggerFactory.CreateLogger<ChunkedEntityLoader<TestDbContext, SimpleEntity>>()
        );
    }
}
/*
         IDbContextFactory<TDbContext> dbContextFactory,
        int chunkSize,
        int maxConcurrentProducerCount,
        Func<TDbContext, IOrderedQueryable<TEntity>> sourceQueryProvider,
        ChunkedEntityLoaderOptions options = ChunkedEntityLoaderOptions.None,
        ILoggerFactory? loggerFactory = null,
        ILogger<ChunkedEntityLoader<TDbContext, TEntity>>? logger = null
 */
