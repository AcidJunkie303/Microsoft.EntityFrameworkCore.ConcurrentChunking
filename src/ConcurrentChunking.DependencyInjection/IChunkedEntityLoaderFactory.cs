using System.Diagnostics.CodeAnalysis;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.DependencyInjection;

/// <summary>
///     Factory interface for creating <see cref="IChunkedEntityLoader{TEntity}" /> instances
///     using a specified <see cref="DbContext" />.
/// </summary>
/// <typeparam name="TDbContext">The type of <see cref="DbContext" />.</typeparam>
[SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters")]
public interface IChunkedEntityLoaderFactory<TDbContext>
    where TDbContext : DbContext
{
    /// <summary>
    ///     Creates a new <see cref="IChunkedEntityLoader{TEntity}" /> for the specified entity type.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to load.</typeparam>
    /// <param name="chunkSize">The number of entities to load per chunk. Must be at least 1.</param>
    /// <param name="maxConcurrentProducerCount">The maximum number of concurrent producers. Must be at least 1.</param>
    /// <param name="maxPrefetchCount">The maximum number of chunks to prefetch. Must be at least 1.</param>
    /// <param name="sourceQueryProvider">
    ///     A function that provides the ordered source query for the entity type.
    ///     The ordering must be deterministic and use unique column(s) (single unique key or unique key combination)
    ///     because chunking relies on <c>Skip</c>/<c>Take</c> pagination.
    ///     It is the caller's responsibility to ensure the ordering includes unique columns.
    /// </param>
    /// <param name="startProducingChunkCallback">Callback for when a chunk is being produced.</param>
    /// <param name="endProducingChunkCallback">Callback for when a chunk was produced.</param>
    /// <param name="options">Loader options.</param>
    /// <param name="useLogging">Whether to enable logging.</param>
    /// <returns>An <see cref="IChunkedEntityLoader{TEntity}" /> instance.</returns>
    [SuppressMessage("Critical Code Smell", "S2360:Optional parameters should not be used")]
    IChunkedEntityLoader<TEntity> Create<TEntity>
    (
        int chunkSize,
        int maxConcurrentProducerCount,
        int maxPrefetchCount,
        Func<TDbContext, IOrderedQueryable<TEntity>> sourceQueryProvider,
        Func<ICallbackArgs<TDbContext>, Task>? startProducingChunkCallback = null,
        Func<ICallbackArgs<TDbContext>, Task>? endProducingChunkCallback = null,
        ChunkedEntityLoaderOptions options = ChunkedEntityLoaderOptions.PreserveChunkOrder,
        bool useLogging = true
    )
        where TEntity : class;
}
