using System.Diagnostics.CodeAnalysis;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking;

[SuppressMessage("Critical Code Smell", "S2360:Optional parameters should not be used")]
public interface IChunkedEntityLoaderFactory<out TContext>
    where TContext : DbContext
{
    IChunkedEntityLoader<TEntity> Create<TEntity>
    (
        Func<TContext, IOrderedQueryable<TEntity>> sourceQueryProvider,
        int chunkSize,
        int maxConcurrentProducerCount,
        ChunkedEntityLoaderOptions options = ChunkedEntityLoaderOptions.None
    )
        where TEntity : class;
}
