using System.Diagnostics.CodeAnalysis;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.DependencyInjection;

public interface IChunkedEntityLoaderFactory<out TDbContext>
    where TDbContext : DbContext
{
    [SuppressMessage("Critical Code Smell", "S2360:Optional parameters should not be used")]
    IChunkedEntityLoader<TEntity> Create<TEntity>
    (
        int chunkSize,
        int maxConcurrentProducerCount,
        int maxPrefetchCount,
        Func<TDbContext, IOrderedQueryable<TEntity>> sourceQueryProvider,
        ChunkedEntityLoaderOptions options = ChunkedEntityLoaderOptions.None,
        bool useLogging = true
    )
        where TEntity : class;
}
