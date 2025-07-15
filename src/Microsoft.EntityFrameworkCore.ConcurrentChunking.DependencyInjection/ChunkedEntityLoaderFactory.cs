using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.DependencyInjection;

[SuppressMessage("Minor Code Smell", "S3416:Loggers should be named for their enclosing types")]
internal sealed class ChunkedEntityLoaderFactory<TDbContext> : IChunkedEntityLoaderFactory<TDbContext>
    where TDbContext : DbContext
{
    private readonly IDbContextFactory<TDbContext> _dbContextFactory;
    private readonly ILoggerFactory _loggerFactory;

    public ChunkedEntityLoaderFactory(IDbContextFactory<TDbContext> dbContextFactory, ILoggerFactory loggerFactory)
    {
        _dbContextFactory = dbContextFactory;
        _loggerFactory = loggerFactory;
    }

    public IChunkedEntityLoader<TEntity> Create<TEntity>
    (
        int chunkSize,
        int maxConcurrentProducerCount,
        int maxPrefetchCount,
        Func<TDbContext, IOrderedQueryable<TEntity>> sourceQueryProvider,
        ChunkedEntityLoaderOptions options = ChunkedEntityLoaderOptions.None,
        bool useLogging = true
    )
        where TEntity : class
    {
        var logger = useLogging
            ? _loggerFactory.CreateLogger<ChunkedEntityLoader<TDbContext, TEntity>>()
            : null;

        return new ChunkedEntityLoader<TDbContext, TEntity>
        (
            _dbContextFactory,
            chunkSize,
            maxConcurrentProducerCount,
            maxPrefetchCount,
            sourceQueryProvider,
            options,
            _loggerFactory,
            logger
        );
    }
}
