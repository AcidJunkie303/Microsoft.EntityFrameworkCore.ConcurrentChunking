using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking;

public static class OrderedQueryableExtensions
{
    public static IAsyncEnumerable<Chunk<TEntity>> LoadChunkedAsync<TEntity, TDbContext>
    (
        this IOrderedQueryable<TEntity> query,
        IDbContextFactory<TDbContext> dbContextFactory,
        int chunkSize,
        int maxDegreeOfParallelism
    )
        where TEntity : class
        where TDbContext : DbContext
    {
        return LoadChunkedAsync
        (
            query,
            dbContextFactory.CreateDbContext,
            chunkSize,
            maxDegreeOfParallelism,
            ChunkedEntityLoaderOptions.None,
            null,
            CancellationToken.None);
    }

    public static IAsyncEnumerable<Chunk<TEntity>> LoadChunkedAsync<TEntity, TDbContext>
    (
        this IOrderedQueryable<TEntity> query,
        Func<TDbContext> dbContextFactory,
        int chunkSize,
        int maxDegreeOfParallelism
    )
        where TEntity : class
        where TDbContext : DbContext
        => LoadChunkedAsync(query, dbContextFactory, chunkSize, maxDegreeOfParallelism, ChunkedEntityLoaderOptions.None, null, CancellationToken.None);

    public static IAsyncEnumerable<Chunk<TEntity>> LoadChunkedAsync<TEntity, TDbContext>
    (
        this IOrderedQueryable<TEntity> query,
        IDbContextFactory<TDbContext> dbContextFactory,
        int chunkSize,
        int maxDegreeOfParallelism,
        ChunkedEntityLoaderOptions options,
        ILoggerFactory? loggerFactory,
        in CancellationToken cancellationToken
    )
        where TEntity : class
        where TDbContext : DbContext
        => LoadChunkedAsync(query, dbContextFactory.CreateDbContext, chunkSize, maxDegreeOfParallelism, options, loggerFactory, cancellationToken);

    public static async IAsyncEnumerable<Chunk<TEntity>> LoadChunkedAsync<TEntity, TDbContext>
    (
        this IOrderedQueryable<TEntity> query,
        Func<TDbContext> dbContextFactory,
        int chunkSize,
        int maxDegreeOfParallelism,
        ChunkedEntityLoaderOptions options,
        ILoggerFactory? loggerFactory,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
        where TEntity : class
        where TDbContext : DbContext
    {
        var entityQueryRootExpression = EntityQueryRootExpressionExtractor.Extract(query.Expression);
        if (entityQueryRootExpression is null)
        {
            throw new IOException("EntityQueryRootExpressionExtractor failed to extract the root expression from the query.");
        }

        await using var newDbContext = dbContextFactory();
        var rootEntityType = entityQueryRootExpression.EntityType.ClrType;
        var untypedDbSetFactory = DbSetAccessor.Get(newDbContext, rootEntityType);
        var dbSetFactory = (Func<DbContext, DbSet<TEntity>>) untypedDbSetFactory;
        var factory = new DbContextFactoryForFunc<DbContext>(dbContextFactory);

        using ChunkedEntityLoader<DbContext, TEntity> loader = new
        (
            dbContextFactory: factory,
            chunkSize: chunkSize,
            maxConcurrentProducerCount: maxDegreeOfParallelism,
            sourceQueryProvider: ctx => CreateQueryOnNewDbContext(ctx, query, dbSetFactory),
            options: options,
            loggerFactory: loggerFactory,
            logger: loggerFactory?.CreateLogger<ChunkedEntityLoader<DbContext, TEntity>>()
        );

        await foreach (var chunk in loader.LoadAsync(cancellationToken))
        {
            yield return chunk;
        }
    }

    private static IOrderedQueryable<TEntity> CreateQueryOnNewDbContext<TDbContext, TEntity>(TDbContext dbContext, IQueryable<TEntity> sourceQuery, Func<DbContext, DbSet<TEntity>> dbSetFactory)
        where TDbContext : DbContext
        where TEntity : class
    {
        var dbSet = dbSetFactory(dbContext);

        return (IOrderedQueryable<TEntity>) dbSet
                                           .AsQueryable()
                                           .Provider
                                           .CreateQuery<TEntity>(sourceQuery.Expression);
    }
}
