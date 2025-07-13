using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore.ConcurrentChunking.Support;
using Microsoft.Extensions.Logging;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking;

/// <summary>
/// Provides extension methods for chunked asynchronous loading of entities from a queryable source.
/// </summary>
[SuppressMessage("Critical Code Smell", "S2360:Optional parameters should not be used", Justification = "This would result is a lot of overloads.")]
[SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters")]
public static class QueryableExtensions
{
    /// <summary>
    /// Loads entities in chunks asynchronously using a database context factory.
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
    /// Loads entities in chunks asynchronously using a function to create database contexts.
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
            sourceQueryProvider: ctx => QueryFactory.Create(ctx, rootEntityType, query),
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

    private static class QueryFactory
    {
        public static IOrderedQueryable<TResultEntity> Create<TDbContext, TResultEntity>(TDbContext dbContext, Type sourceEntityType, IQueryable<TResultEntity> sourceQuery)
            where TDbContext : DbContext
            where TResultEntity : class
        {
            var type = typeof(QueryFactory<,,>).MakeGenericType(typeof(DbContext), typeof(TResultEntity), sourceEntityType);
            var factory = (IQueryFactory<TDbContext, TResultEntity>) Activator.CreateInstance(type)!;
            return factory.CreateQueryOnNewDbContext(dbContext, sourceQuery);
        }
    }

    private interface IQueryFactory<in TDbContext, TResultEntity>
        where TDbContext : DbContext
        where TResultEntity : class
    {
        IOrderedQueryable<TResultEntity> CreateQueryOnNewDbContext(TDbContext dbContext, IQueryable<TResultEntity> sourceQuery);
    }

    [SuppressMessage("Major Code Smell", "S2436:Types and methods should not have too many generic parameters")]
    private sealed class QueryFactory<TDbContext, TResultEntity, TSourceEntity> : IQueryFactory<TDbContext, TResultEntity>
        where TDbContext : DbContext
        where TResultEntity : class
        where TSourceEntity : class
    {
        private static readonly Func<TDbContext, DbSet<TSourceEntity>> DbSetAccessor = CreateDbSetAccessor();

        public IOrderedQueryable<TResultEntity> CreateQueryOnNewDbContext
        (
            TDbContext dbContext,
            IQueryable<TResultEntity> sourceQuery
        )
        {
            return (IOrderedQueryable<TResultEntity>) DbSetAccessor(dbContext)
                                                     .AsQueryable()
                                                     .Provider
                                                     .CreateQuery(sourceQuery.Expression);
        }

        private static Func<TDbContext, DbSet<TSourceEntity>> CreateDbSetAccessor()
        {
            var parameter = Expression.Parameter(typeof(TDbContext), "dbContext");
            var methodInfo = typeof(DbContext).GetMethod("Set", [])
                             ?? throw new InvalidOperationException($"Could not find {nameof(DbContext)}.{nameof(DbContext.Set)}() method.");
            var genericMethodInfo = methodInfo.MakeGenericMethod(typeof(TSourceEntity));

            var lambda = Expression.Lambda<Func<TDbContext, DbSet<TSourceEntity>>>
            (
                Expression.Call(parameter, genericMethodInfo),
                parameter
            );

            return lambda.Compile();
        }
    }
}
