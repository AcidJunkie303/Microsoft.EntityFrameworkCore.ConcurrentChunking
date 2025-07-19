using System.Diagnostics.CodeAnalysis;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.DependencyInjection;

/// <summary>
///     Factory interface for creating <see cref="IChunkedEntityLoader{TEntity}" /> instances
///     using a specified <see cref="DbContext" />.
/// </summary>
/// <typeparam name="TDbContext">The type of <see cref="DbContext" />.</typeparam>
public interface IChunkedEntityLoaderFactory<out TDbContext>
    where TDbContext : DbContext
{
    /// <summary>
    ///     Creates a new <see cref="IChunkedEntityLoader{TEntity}" /> for the specified entity type.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to load.</typeparam>
    /// <param name="chunkSize">The number of entities to load per chunk.</param>
    /// <param name="maxConcurrentProducerCount">The maximum number of concurrent producers.</param>
    /// <param name="maxPrefetchCount">The maximum number of chunks to prefetch.</param>
    /// <param name="sourceQueryProvider">A function that provides the source query for the entity type.</param>
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
        ChunkedEntityLoaderOptions options = ChunkedEntityLoaderOptions.None,
        bool useLogging = true
    )
        where TEntity : class;
}
