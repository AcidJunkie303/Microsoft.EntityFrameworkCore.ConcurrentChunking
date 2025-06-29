using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking;

// TODO:
// - add post-filtering support

public sealed class ChunkedEntityLoader<TDbContext, TEntity> : IChunkedEntityLoader<TEntity>
    where TDbContext : DbContext
    where TEntity : class
{
    private static readonly string EntityTypeName = typeof(TEntity).Name;
    private readonly Func<TDbContext, IOrderedQueryable<TEntity>> _sourceQueryProvider;
    private readonly ChunkedEntityLoaderOptions _options;
    private readonly ILogger<ChunkedEntityLoader<TDbContext, TEntity>>? _logger;
    private readonly ILoggerFactory? _loggerFactory;
    private readonly Channel<Chunk<TEntity>> _channel;
    private readonly Func<TDbContext> _dbContextFactory;
    private readonly SemaphoreSlim _producerLimiterSemaphore;
    private readonly int _chunkSize;

    [SuppressMessage("Critical Code Smell", "S2360:Optional parameters should not be used")]
    public ChunkedEntityLoader
    (
        IDbContextFactory<TDbContext> dbContextFactory,
        int chunkSize,
        int maxConcurrentProducerCount,
        Func<TDbContext, IOrderedQueryable<TEntity>> sourceQueryProvider,
        ChunkedEntityLoaderOptions options = ChunkedEntityLoaderOptions.None,
        ILoggerFactory? loggerFactory = null,
        ILogger<ChunkedEntityLoader<TDbContext, TEntity>>? logger = null
    )
        : this
        (
            dbContextFactory.CreateDbContext,
            chunkSize,
            maxConcurrentProducerCount,
            sourceQueryProvider,
            options,
            loggerFactory,
            logger
        )
    {
    }

    [SuppressMessage("Critical Code Smell", "S2360:Optional parameters should not be used")]
    public ChunkedEntityLoader
    (
        Func<TDbContext> dbContextFactory,
        int chunkSize,
        int maxConcurrentProducerCount,
        Func<TDbContext, IOrderedQueryable<TEntity>> sourceQueryProvider,
        ChunkedEntityLoaderOptions options = ChunkedEntityLoaderOptions.None,
        ILoggerFactory? loggerFactory = null,
        ILogger<ChunkedEntityLoader<TDbContext, TEntity>>? logger = null
    )
    {
        _sourceQueryProvider = sourceQueryProvider;
        _loggerFactory = loggerFactory;
        _options = options;
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _chunkSize = chunkSize;
        _producerLimiterSemaphore = new SemaphoreSlim(maxConcurrentProducerCount, maxConcurrentProducerCount);

        var channelOptions = new BoundedChannelOptions(maxConcurrentProducerCount)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _channel = Channel.CreateBounded<Chunk<TEntity>>(channelOptions);
    }

    public async IAsyncEnumerable<Chunk<TEntity>> LoadAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var producersTask = StartProducersAsync(cancellationToken);
        var channelReader = CreateChannelReader();

        await foreach (var chunk in channelReader.ReadAsync(cancellationToken))
        {
            yield return chunk;
        }

        await producersTask;
    }

    public void Dispose() => _producerLimiterSemaphore.Dispose();

    private async Task StartProducersAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();

        var entityCount = await GetExpectedEntityCoundAsync();
        var chunkCount = (int) (entityCount / _chunkSize) + (entityCount % _chunkSize > 0 ? 1 : 0);

        _logger?.LogTrace("Starting chunked entity loader for EntityTypeName={EntityTypeName} with ChunkSize={ChunkSize}, MaxConcurrentProducerCount={MaxConcurrentProducerCount}, ExpectedEntityCount={ExpectedEntityCount}, ChunkCount={ChunkCount}.",
            EntityTypeName, _chunkSize, _producerLimiterSemaphore.CurrentCount, entityCount, chunkCount);

        var tasks = new List<Task>(chunkCount);

        for (var i = 0; i < chunkCount; i++)
        {
            var currentChunkIndex = i;
            await _producerLimiterSemaphore.WaitAsync(cancellationToken);

            var task = Task.Run(() => ProduceAndReleaseSemaphoreAsync(currentChunkIndex, cancellationToken), cancellationToken); // we do not await this task to allow concurrent production
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        await _channel.Writer.WriteAsync(null!, cancellationToken);
        _channel.Writer.Complete();
    }

    private async Task ProduceAndReleaseSemaphoreAsync(int chunkIndex, CancellationToken cancellationToken)
    {
        try
        {
            await ProduceAsync(chunkIndex, cancellationToken);
        }
        finally
        {
            _producerLimiterSemaphore.Release();
        }
    }

    private async Task ProduceAsync(int chunkIndex, CancellationToken cancellationToken)
    {
        await using var context = _dbContextFactory();
        var query = _sourceQueryProvider(context);
        var startIndex = chunkIndex * _chunkSize;

        _logger?.LogTrace("Producing chunk #{ChunkIndex} with StartIndex={StartIndex} for EntityTypeName={EntityTypeName}", chunkIndex, startIndex, EntityTypeName);

        var startedTimestamp = Stopwatch.GetTimestamp();
        var entities = await query
                            .Skip(startIndex)
                            .Take(_chunkSize)
                            .ToListAsync(cancellationToken);

        _logger?.LogTrace("Produced chunk #{ChunkIndex} with StartIndex={StartIndex} for EntityTypeName={EntityTypeName} with {EntityCount} entities in {DurationInMs} ms.", chunkIndex, startIndex, EntityTypeName, entities.Count, (int) Stopwatch.GetElapsedTime(startedTimestamp).TotalMilliseconds);

        var chunk = new Chunk<TEntity>(chunkIndex, entities);
        await _channel.Writer.WriteAsync(chunk, cancellationToken);
    }

    private IChannelReader<TEntity> CreateChannelReader()
        => _options.HasFlag(ChunkedEntityLoaderOptions.PreserveChunkOrder)
            ? new OrderedChannelReader<TEntity>(_channel.Reader, _loggerFactory?.CreateLogger<OrderedChannelReader<TEntity>>())
            : new UnorderedChannelReader<TEntity>(_channel.Reader, _loggerFactory?.CreateLogger<UnorderedChannelReader<TEntity>>());

    private async Task<long> GetExpectedEntityCoundAsync()
    {
        await using var context = _dbContextFactory();

        _logger?.LogTrace("Getting total entity expected entity count for EntityTypeName={EntityTypeName}", EntityTypeName);
        var count = await _sourceQueryProvider(context).LongCountAsync();

        _logger?.LogTrace("Expected entity count for EntityTypeName={EntityTypeName} is {EntityCount}.", EntityTypeName, count);
        return count;
    }
}
