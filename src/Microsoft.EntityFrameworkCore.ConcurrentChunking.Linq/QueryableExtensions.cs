using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.Linq;

/// <summary>
///     Provides extension methods for chunked asynchronous loading of entities from a queryable source.
/// </summary>
[SuppressMessage("Critical Code Smell", "S2360:Optional parameters should not be used", Justification = "This would result is a lot of overloads.")]
[SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters")]
public static class QueryableExtensions
{
    /// <summary>
    ///     Loads entities in chunks asynchronously using a database context factory.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity being loaded.</typeparam>
    /// <typeparam name="TDbContext">The type of the database context.</typeparam>
    /// <param name="query">The queryable source of entities.</param>
    /// <param name="dbContextFactory">The factory to create database contexts.</param>
    /// <param name="chunkSize">The size of each chunk.</param>
    /// <param name="maxDegreeOfParallelism">The maximum number of concurrent producers.</param>
    /// <param name="maxPrefetchCount">The maximum number of chunks to prefetch.</param>
    /// <param name="options">Options for the chunked entity loader.</param>
    /// <param name="loggerFactory">Optional logger factory.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>An asynchronous enumerable of chunks containing entities.</returns>
    public static IAsyncEnumerable<Chunk<TEntity>> LoadChunkedAsync<TEntity, TDbContext>
    (
        this IQueryable<TEntity> query,
        IDbContextFactory<TDbContext> dbContextFactory,
        int chunkSize,
        int maxDegreeOfParallelism,
        int maxPrefetchCount,
        ChunkedEntityLoaderOptions options = ChunkedEntityLoaderOptions.None,
        ILoggerFactory? loggerFactory = null,
        in CancellationToken cancellationToken = default
    )
        where TEntity : class
        where TDbContext : DbContext
        => LoadChunkedAsync
        (
            query,
            dbContextFactory.CreateDbContext,
            chunkSize,
            maxDegreeOfParallelism,
            maxPrefetchCount,
            options,
            loggerFactory,
            cancellationToken
        );

    /// <summary>
    ///     Loads entities in chunks asynchronously using a function to create database contexts.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity being loaded.</typeparam>
    /// <typeparam name="TDbContext">The type of the database context.</typeparam>
    /// <param name="query">The queryable source of entities.</param>
    /// <param name="dbContextFactory">The function to create database contexts.</param>
    /// <param name="chunkSize">The size of each chunk.</param>
    /// <param name="maxDegreeOfParallelism">The maximum number of concurrent producers.</param>
    /// <param name="maxPrefetchCount">The maximum number of chunks to prefetch.</param>
    /// <param name="options">Options for the chunked entity loader.</param>
    /// <param name="loggerFactory">Optional logger factory.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>An asynchronous enumerable of chunks containing entities.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static async IAsyncEnumerable<Chunk<TEntity>> LoadChunkedAsync<TEntity, TDbContext>
    (
        this IQueryable<TEntity> query,
        Func<TDbContext> dbContextFactory,
        int chunkSize,
        int maxDegreeOfParallelism,
        int maxPrefetchCount,
        ChunkedEntityLoaderOptions options = ChunkedEntityLoaderOptions.None,
        ILoggerFactory? loggerFactory = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
        where TEntity : class
        where TDbContext : DbContext
    {
        EnsureIsOrderedQuery(query.Expression);

        var entityQueryRootExpression = EntityQueryRootExpressionExtractor.Extract(query.Expression)
                                        ?? throw new InvalidOperationException("EntityQueryRootExpressionExtractor failed to extract the root expression from the query.");
        await using var newDbContext = dbContextFactory();
        var rootEntityType = entityQueryRootExpression.EntityType.ClrType;

        using ChunkedEntityLoader<TDbContext, TEntity> loader = new
        (
            dbContextFactory: new DbContextFactoryForFunc<TDbContext>(dbContextFactory),
            chunkSize: chunkSize,
            maxConcurrentProducerCount: maxDegreeOfParallelism,
            maxPrefetchCount: maxPrefetchCount,
            sourceQueryProvider: ctx => CloneQueryToDbContext(ctx, rootEntityType, query),
            options: options,
            loggerFactory: loggerFactory,
            logger: loggerFactory?.CreateLogger<ChunkedEntityLoader<TDbContext, TEntity>>()
        );

        await foreach (var chunk in loader.LoadAsync(cancellationToken))
        {
            yield return chunk;
        }
    }

    private static void EnsureIsOrderedQuery(Expression expression)
    {
        if (QueryExpressionChecker.HasOrderBy(expression))
        {
            return;
        }

        throw new InvalidOperationException($"The query must have a '{nameof(Queryable.OrderBy)}' or '{nameof(Queryable.OrderByDescending)}' clause to ensure consistent chunking.");
    }

    private static IOrderedQueryable<TResultEntity> CloneQueryToDbContext<TResultEntity>(DbContext dbContext, Type entityType, IQueryable<TResultEntity> sourceQuery)
        where TResultEntity : class
    {
        var dbSetAccessor = DbSetAccessorFactory.CreateDbSetAccessor(entityType);
        var queryable = dbSetAccessor(dbContext);

        return (IOrderedQueryable<TResultEntity>) queryable
                                                 .Provider
                                                 .CreateQuery(sourceQuery.Expression);
    }
}
