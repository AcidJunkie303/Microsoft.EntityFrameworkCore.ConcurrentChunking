using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ConcurrentChunking;
using Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Data;
using Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Entities;
using Xunit;

namespace ConcurrentChunking.Testing;

#pragma warning disable MA0048 // File name must match type name

public abstract partial class ChunkedEntityLoaderTestBase<TDbContext, TTestData> : TestBase
    where TDbContext : DbContext, IDbContext, new()
    where TTestData : ITestData<TDbContext>
{
    private static int EntityCount => TTestData.EntityCount;

    protected ChunkedEntityLoaderTestBase(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    protected ChunkedEntityLoader<TDbContext, SimpleEntity> CreateLoader
    (
        int chunkSize,
        int maxConcurrentProducerCount,
        int maxPrefetchCount,
        ChunkedEntityLoaderOptions options,
        Func<IStartCallbackArgs<TDbContext>, Task<object?>>? startProducingChunkCallback = null,
        Func<IEndCallbackArgs<TDbContext>, Task>? endProducingChunkCallback = null
    )
    {
        return new ChunkedEntityLoader<TDbContext, SimpleEntity>(
            dbContextFactory: () => new TDbContext(),
            chunkSize: chunkSize,
            maxConcurrentProducerCount: maxConcurrentProducerCount,
            maxPrefetchCount: maxPrefetchCount,
            startProducingChunkCallback: startProducingChunkCallback,
            endProducingChunkCallback: endProducingChunkCallback,
            sourceQueryProvider: ctx => ctx.SimpleEntities.AsNoTracking().OrderBy(e => e.Id),
            options: options,
            logger: LoggerFactory.CreateLogger<ChunkedEntityLoader<TDbContext, SimpleEntity>>()
        );
    }

    private static bool IsChunkOrderSequential<T>(in List<Chunk<T>> chunks)
        => !chunks.Where((chunk, i) => chunk.ChunkIndex != i).Any();
}
