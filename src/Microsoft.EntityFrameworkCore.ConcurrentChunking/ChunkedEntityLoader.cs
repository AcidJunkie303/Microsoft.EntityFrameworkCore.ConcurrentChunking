using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking;

// TODO:
// - add post-filtering support

public sealed class ChunkedEntityLoader<TContext, TEntity> : IChunkedEntityLoader<TEntity>
    where TContext : DbContext
    where TEntity : class
{
    private static readonly string EntityTypeName = typeof(TEntity).Name;
    private readonly Func<TContext, IOrderedQueryable<TEntity>> _sourceQueryProvider;
    private readonly ChunkedEntityLoaderOptions _options;
    private readonly ILogger<ChunkedEntityLoader<TContext, TEntity>>? _logger;
    private readonly ILoggerFactory? _loggerFactory;
    private readonly Channel<Chunk<TEntity>> _channel;
    private readonly IDbContextFactory<TContext> _dbContextFactory;
    private readonly SemaphoreSlim _producerLimiterSemaphore;
    private readonly int _chunkSize;

    [SuppressMessage("Critical Code Smell", "S2360:Optional parameters should not be used")]
    public ChunkedEntityLoader
    (
        IDbContextFactory<TContext> dbContextFactory,
        int chunkSize,
        int maxConcurrentProducerCount,
        Func<TContext, IOrderedQueryable<TEntity>> sourceQueryProvider,
        ChunkedEntityLoaderOptions options = ChunkedEntityLoaderOptions.None,
        ILoggerFactory? loggerFactory = null,
        ILogger<ChunkedEntityLoader<TContext, TEntity>>? logger = null
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
        var chunkCount = (entityCount / _chunkSize) + (entityCount % _chunkSize > 0 ? 1 : 0);

        for (var i = 0; i < chunkCount; i++)
        {
            await _producerLimiterSemaphore.WaitAsync(cancellationToken);

            try
            {
                await ProduceAsync(i, cancellationToken);
            }
            finally
            {
                _producerLimiterSemaphore.Release();
            }
        }

        await _channel.Writer.WriteAsync(null!, cancellationToken);
        _channel.Writer.Complete();
    }

    private async Task ProduceAsync(int chunkIndex, CancellationToken cancellationToken)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var query = _sourceQueryProvider(context);
        var startIndex = chunkIndex * _chunkSize;

        _logger?.LogTrace("Producing chunk #{ChunkIndex} with {StartIndex} for {EntityTypeName}", chunkIndex, startIndex, EntityTypeName);

        var startedTimestamp = Stopwatch.GetTimestamp();
        var entities = await query
                            .Skip(startIndex)
                            .Take(_chunkSize)
                            .ToListAsync(cancellationToken);

        _logger?.LogTrace("Produced chunk #{ChunkIndex} with {StartIndex} for {EntityTypeName} with {EntityCount} entities. Duration {DurationInMs} ms.", chunkIndex, startIndex, EntityTypeName, entities.Count, Stopwatch.GetElapsedTime(startedTimestamp).TotalMilliseconds);

        var chunk = new Chunk<TEntity>(chunkIndex, entities);
        await _channel.Writer.WriteAsync(chunk, cancellationToken);
    }

    private IChannelReader<TEntity> CreateChannelReader()
        => _options.HasFlag(ChunkedEntityLoaderOptions.PreserveChunkOrder)
            ? new OrderedChannelReader<TEntity>(_channel.Reader, _loggerFactory?.CreateLogger<OrderedChannelReader<TEntity>>())
            : new UnorderedChannelReader<TEntity>(_channel.Reader, _loggerFactory?.CreateLogger<UnorderedChannelReader<TEntity>>());

    private async Task<int> GetExpectedEntityCoundAsync()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await _sourceQueryProvider(context).CountAsync();
    }
}
