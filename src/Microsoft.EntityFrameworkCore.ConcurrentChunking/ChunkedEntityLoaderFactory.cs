using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking;

public sealed class ChunkedEntityLoaderFactory<TContext> : IChunkedEntityLoaderFactory<TContext>
    where TContext : DbContext
{
    private readonly IDbContextFactory<TContext> _dbContextFactory;
    private readonly ILoggerFactory _loggerFactory;

    public ChunkedEntityLoaderFactory(IDbContextFactory<TContext> dbContextFactory, ILoggerFactory loggerFactory)
    {
        _dbContextFactory = dbContextFactory;
        _loggerFactory = loggerFactory;
    }

    [SuppressMessage("Critical Code Smell", "S2360:Optional parameters should not be used")]
    public IChunkedEntityLoader<TEntity> Create<TEntity>
    (
        Func<TContext, IOrderedQueryable<TEntity>> sourceQueryProvider,
        int chunkSize,
        int maxConcurrentProducerCount,
        ChunkedEntityLoaderOptions options = ChunkedEntityLoaderOptions.None
    )
        where TEntity : class
        => new ChunkedEntityLoader<TContext, TEntity>
        (
            _dbContextFactory,
            chunkSize,
            maxConcurrentProducerCount,
            sourceQueryProvider,
            options,
            _loggerFactory.CreateLogger<ChunkedEntityLoader<TContext, TEntity>>()
        );
}
