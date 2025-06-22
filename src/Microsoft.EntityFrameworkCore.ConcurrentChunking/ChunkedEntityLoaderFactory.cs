using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking;

/// <summary>
///     Factory for creating instances of <see cref="IChunkedEntityLoader{TEntity}" /> for a specific entity framework
///     <see cref="DbContext" /> type.
/// </summary>
/// <typeparam name="TContext">The entity framework <see cref="DbContext" /> type.</typeparam>
public sealed class ChunkedEntityLoaderFactory<TContext> : IChunkedEntityLoaderFactory<TContext>
    where TContext : DbContext
{
    private readonly IDbContextFactory<TContext> _dbContextFactory;
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    ///     Creates a new instance of <see cref="ChunkedEntityLoaderFactory{TContext}" /> with the specified
    ///     <paramref name="dbContextFactory" /> and <paramref name="loggerFactory" />.
    /// </summary>
    /// <param name="dbContextFactory">The <see cref="DbContext" /> factory.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    public ChunkedEntityLoaderFactory(IDbContextFactory<TContext> dbContextFactory, ILoggerFactory loggerFactory)
    {
        _dbContextFactory = dbContextFactory;
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    ///     Creates a new instance of the chunked entity loader for the specified entity type <typeparamref name="TEntity" />.
    /// </summary>
    /// <param name="sourceQueryProvider">The source query loader.</param>
    /// <param name="chunkSize">The chunk size.</param>
    /// <param name="maxConcurrentProducerCount">The maximum concurrent entity chunk producer count.</param>
    /// <param name="options">The options of the chunked entity loader.</param>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <returns>A new instance of the chunked entity loader.</returns>
    [SuppressMessage("Critical Code Smell", "S2360:Optional parameters should not be used")]
    public IChunkedEntityLoader<TEntity> Create<TEntity>
    (
        Func<TContext, IOrderedQueryable<TEntity>> sourceQueryProvider,
        int chunkSize,
        int maxConcurrentProducerCount,
        ChunkedEntityLoaderOptions options = ChunkedEntityLoaderOptions.None
    )
        where TEntity : class
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(chunkSize);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxConcurrentProducerCount);

        return new ChunkedEntityLoader<TContext, TEntity>
        (
            dbContextFactory: _dbContextFactory,
            chunkSize: chunkSize,
            maxConcurrentProducerCount: maxConcurrentProducerCount,
            sourceQueryProvider: sourceQueryProvider,
            options: options,
            loggerFactory: _loggerFactory,
            logger: _loggerFactory.CreateLogger<ChunkedEntityLoader<TContext, TEntity>>()
        );
    }
}
