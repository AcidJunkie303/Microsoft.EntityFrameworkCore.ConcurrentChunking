using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.Linq;

/// <summary>
///     Provides extension methods for chunked asynchronous loading of entities from a queryable source.
/// </summary>
[SuppressMessage("Critical Code Smell", "S2360:Optional parameters should not be used", Justification = "This would result in a lot of overloads.")]
[SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters")]
public static class QueryableExtensions
{
    /// <summary>
    ///     Loads entities in chunks asynchronously using a database context factory.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity being loaded.</typeparam>
    /// <typeparam name="TDbContext">The type of the database context.</typeparam>
    /// <param name="query">
    ///     The ordered queryable source of entities.
    ///     The ordering must be deterministic and use unique column(s) (single unique key or unique key combination),
    ///     because chunking relies on <c>Skip</c>/<c>Take</c> pagination.
    ///     It is the caller's responsibility to provide an <see cref="IOrderedQueryable{T}" /> with unique ordering.
    /// </param>
    /// <param name="dbContextFactory">The factory to create database contexts.</param>
    /// <param name="chunkSize">The size of each chunk.</param>
    /// <param name="maxConcurrentProducerCount">The maximum number of concurrent producers.</param>
    /// <param name="maxPrefetchCount">The maximum number of chunks to prefetch.</param>
    /// <param name="options">Options for the chunked entity loader.</param>
    /// <param name="startProducingChunkCallback">Callback for when a chunk is being produced.</param>
    /// <param name="endProducingChunkCallback">Callback for when a chunk was produced.</param>
    /// <param name="loggerFactory">Optional logger factory.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>An asynchronous enumerable of chunks containing entities.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="query" /> or <paramref name="dbContextFactory" />
    ///     is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the query does not include an explicit <c>OrderBy</c>/
    ///     <c>OrderByDescending</c>.
    /// </exception>
    public static IAsyncEnumerable<Chunk<TEntity>> LoadChunkedAsync<TEntity, TDbContext>
    (
        this IOrderedQueryable<TEntity> query,
        IDbContextFactory<TDbContext> dbContextFactory,
        int chunkSize,
        int maxConcurrentProducerCount,
        int maxPrefetchCount,
        ChunkedEntityLoaderOptions options = ChunkedEntityLoaderOptions.PreserveChunkOrder,
        Func<IStartCallbackArgs<TDbContext>, Task<object?>>? startProducingChunkCallback = null,
        Func<IEndCallbackArgs<TDbContext>, Task>? endProducingChunkCallback = null,
        ILoggerFactory? loggerFactory = null,
        in CancellationToken cancellationToken = default
    )
        where TEntity : class
        where TDbContext : DbContext
        =>
            query.LoadChunkedAsync
            (
                dbContextFactory: dbContextFactory.CreateDbContext,
                chunkSize: chunkSize,
                maxConcurrentProducerCount: maxConcurrentProducerCount,
                maxPrefetchCount: maxPrefetchCount,
                options: options,
                startProducingChunkCallback: startProducingChunkCallback,
                endProducingChunkCallback: endProducingChunkCallback,
                loggerFactory: loggerFactory,
                cancellationToken: cancellationToken
            );

    /// <summary>
    ///     Loads entities in chunks asynchronously using a function to create database contexts.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity being loaded.</typeparam>
    /// <typeparam name="TDbContext">The type of the database context.</typeparam>
    /// <param name="query">
    ///     The ordered queryable source of entities.
    ///     The ordering must be deterministic and use unique column(s) (single unique key or unique key combination),
    ///     because chunking relies on <c>Skip</c>/<c>Take</c> pagination.
    ///     It is the caller's responsibility to provide an <see cref="IOrderedQueryable{T}" /> with unique ordering.
    /// </param>
    /// <param name="dbContextFactory">The function to create database contexts.</param>
    /// <param name="chunkSize">The size of each chunk.</param>
    /// <param name="maxConcurrentProducerCount">The maximum number of concurrent producers.</param>
    /// <param name="maxPrefetchCount">The maximum number of chunks to prefetch.</param>
    /// <param name="options">Options for the chunked entity loader.</param>
    /// <param name="startProducingChunkCallback">Callback for when a chunk is being produced.</param>
    /// <param name="endProducingChunkCallback">Callback for when a chunk was produced.</param>
    /// <param name="loggerFactory">Optional logger factory.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>An asynchronous enumerable of chunks containing entities.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="query" /> or <paramref name="dbContextFactory" />
    ///     is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the query does not include an explicit <c>OrderBy</c>/
    ///     <c>OrderByDescending</c>.
    /// </exception>
    public static IAsyncEnumerable<Chunk<TEntity>> LoadChunkedAsync<TEntity, TDbContext>
    (
        this IOrderedQueryable<TEntity> query,
        Func<TDbContext> dbContextFactory,
        int chunkSize,
        int maxConcurrentProducerCount,
        int maxPrefetchCount,
        ChunkedEntityLoaderOptions options = ChunkedEntityLoaderOptions.PreserveChunkOrder,
        Func<IStartCallbackArgs<TDbContext>, Task<object?>>? startProducingChunkCallback = null,
        Func<IEndCallbackArgs<TDbContext>, Task>? endProducingChunkCallback = null,
        ILoggerFactory? loggerFactory = null,
        in CancellationToken cancellationToken = default
    )
        where TEntity : class
        where TDbContext : DbContext
    {
        ValidateLoadChunkedArguments(query, dbContextFactory);
        return LoadChunkedCoreAsync(query, dbContextFactory, chunkSize, maxConcurrentProducerCount, maxPrefetchCount, options, startProducingChunkCallback, endProducingChunkCallback, loggerFactory, cancellationToken);
    }

    private static async IAsyncEnumerable<Chunk<TEntity>> LoadChunkedCoreAsync<TEntity, TDbContext>
    (
        IOrderedQueryable<TEntity> query,
        Func<TDbContext> dbContextFactory,
        int chunkSize,
        int maxConcurrentProducerCount,
        int maxPrefetchCount,
        ChunkedEntityLoaderOptions options,
        Func<IStartCallbackArgs<TDbContext>, Task<object?>>? startProducingChunkCallback,
        Func<IEndCallbackArgs<TDbContext>, Task>? endProducingChunkCallback,
        ILoggerFactory? loggerFactory,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
        where TEntity : class
        where TDbContext : DbContext
    {
        var entityQueryRootExpression = EntityQueryRootExpressionExtractor.Extract(query.Expression)
                                        ?? throw new InvalidOperationException("EntityQueryRootExpressionExtractor failed to extract the root expression from the query.");
        var rootEntityType = entityQueryRootExpression.EntityType.ClrType;

        ChunkedEntityLoader<TDbContext, TEntity> loader = new
        (
            dbContextFactory: new DbContextFactoryForFunc<TDbContext>(dbContextFactory),
            chunkSize: chunkSize,
            maxConcurrentProducerCount: maxConcurrentProducerCount,
            maxPrefetchCount: maxPrefetchCount,
            sourceQueryProvider: ctx => ApplyQueryToDbContext(ctx, rootEntityType, query),
            options: options,
            startProducingChunkCallback: startProducingChunkCallback,
            endProducingChunkCallback: endProducingChunkCallback,
            logger: loggerFactory?.CreateLogger<ChunkedEntityLoader<TDbContext, TEntity>>()
        );

        await foreach (var chunk in loader.LoadAsync(cancellationToken))
        {
            yield return chunk;
        }
    }

    private static void ValidateLoadChunkedArguments<TEntity, TDbContext>(IOrderedQueryable<TEntity> query, Func<TDbContext> dbContextFactory)
        where TEntity : class
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(dbContextFactory);

        if (!QueryExpressionChecker.HasOrderBy(query.Expression))
        {
            throw new InvalidOperationException("The query must include an explicit OrderBy/OrderByDescending before calling LoadChunkedAsync.");
        }
    }

    private static IOrderedQueryable<TResultEntity> ApplyQueryToDbContext<TResultEntity>(DbContext dbContext, Type entityType, IOrderedQueryable<TResultEntity> sourceQuery)
        where TResultEntity : class
    {
        var dbSetAccessor = DbSetAccessorFactory.CreateDbSetAccessor(entityType);
        var queryable = dbSetAccessor(dbContext);

        var result = queryable
                    .Provider
                    .CreateQuery(sourceQuery.Expression);

        if (result is not IOrderedQueryable<TResultEntity> orderedResult)
        {
            throw new InvalidOperationException(
                "Failed to reconstruct an IOrderedQueryable<" + typeof(TResultEntity).Name + "> from the query expression. " +
                "Ensure the query provided is properly ordered by unique column(s). " +
                "It is the caller's responsibility to provide an IOrderedQueryable with deterministic and unique ordering.");
        }

        return orderedResult;
    }
}
