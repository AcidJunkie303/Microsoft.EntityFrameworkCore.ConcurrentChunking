using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking;

/// <summary>
///     A loader that retrieves entities from a database in chunks, allowing concurrent processing.
/// </summary>
/// <typeparam name="TDbContext">The type of the database context.</typeparam>
/// <typeparam name="TEntity">The type of the entity being loaded.</typeparam>
[SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters")]
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
    private readonly SemaphoreSlim _prefetchLimiterSemaphore;
    private readonly int _chunkSize;
    private int _isUsed;

#pragma warning disable S2325
    private bool HasErrors
#pragma warning restore S2325
    {
        get => Volatile.Read(ref field);
        set => Volatile.Write(ref field, value);
    }

    internal Func<int, Task>? ChunkProductionStarted { get; set; }
    internal StatisticsMonitor? StatisticsMonitor { get; set; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ChunkedEntityLoader{TDbContext, TEntity}" /> class using an
    ///     <see cref="IDbContextFactory{TDbContext}" />.
    /// </summary>
    /// <param name="dbContextFactory">Factory to create database contexts.</param>
    /// <param name="chunkSize">The size of each chunk.</param>
    /// <param name="maxConcurrentProducerCount">Maximum number of concurrent producers.</param>
    /// <param name="maxPrefetchCount">Maximum number of chunks to prefetch.</param>
    /// <param name="sourceQueryProvider">
    ///     Function to provide the ordered query for retrieving entities.
    ///     The ordering must be deterministic and use unique column(s) (single unique key or unique key combination)
    ///     because chunking relies on <c>Skip</c>/<c>Take</c> pagination.
    ///     It is the caller's responsibility to ensure the ordering includes unique columns.
    /// </param>
    /// <param name="options">Loader options.</param>
    /// <param name="loggerFactory">Optional logger factory.</param>
    /// <param name="logger">Optional logger.</param>
    [SuppressMessage("Critical Code Smell", "S2360:Optional parameters should not be used")]
    public ChunkedEntityLoader
    (
        IDbContextFactory<TDbContext> dbContextFactory,
        int chunkSize,
        int maxConcurrentProducerCount,
        int maxPrefetchCount,
        Func<TDbContext, IOrderedQueryable<TEntity>> sourceQueryProvider,
        ChunkedEntityLoaderOptions options = ChunkedEntityLoaderOptions.PreserveChunkOrder,
        ILoggerFactory? loggerFactory = null,
        ILogger<ChunkedEntityLoader<TDbContext, TEntity>>? logger = null
    )
        : this
        (
            dbContextFactory.CreateDbContext,
            chunkSize,
            maxConcurrentProducerCount,
            maxPrefetchCount,
            sourceQueryProvider,
            options,
            loggerFactory,
            logger
        )
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ChunkedEntityLoader{TDbContext, TEntity}" /> class using a function to
    ///     create database contexts.
    /// </summary>
    /// <param name="dbContextFactory">Function to create database contexts.</param>
    /// <param name="chunkSize">The size of each chunk.</param>
    /// <param name="maxConcurrentProducerCount">Maximum number of concurrent producers.</param>
    /// <param name="maxPrefetchCount">Maximum number of chunks to prefetch.</param>
    /// <param name="sourceQueryProvider">
    ///     Function to provide the ordered query for retrieving entities.
    ///     The ordering must be deterministic and use unique column(s) (single unique key or unique key combination)
    ///     because chunking relies on <c>Skip</c>/<c>Take</c> pagination.
    ///     It is the caller's responsibility to ensure the ordering includes unique columns.
    /// </param>
    /// <param name="options">Loader options.</param>
    /// <param name="loggerFactory">Optional logger factory.</param>
    /// <param name="logger">Optional logger.</param>
    [SuppressMessage("Critical Code Smell", "S2360:Optional parameters should not be used")]
    public ChunkedEntityLoader
    (
        Func<TDbContext> dbContextFactory,
        int chunkSize,
        int maxConcurrentProducerCount,
        int maxPrefetchCount,
        Func<TDbContext, IOrderedQueryable<TEntity>> sourceQueryProvider,
        ChunkedEntityLoaderOptions options = ChunkedEntityLoaderOptions.PreserveChunkOrder,
        ILoggerFactory? loggerFactory = null,
        ILogger<ChunkedEntityLoader<TDbContext, TEntity>>? logger = null
    )
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);
        ArgumentOutOfRangeException.ThrowIfLessThan(chunkSize, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxConcurrentProducerCount, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxPrefetchCount, 1);
        ArgumentNullException.ThrowIfNull(sourceQueryProvider);

        _sourceQueryProvider = sourceQueryProvider;
        _loggerFactory = loggerFactory;
        _options = options;
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _chunkSize = chunkSize;

        var finalMaxConcurrentProducerCount = Math.Min(maxConcurrentProducerCount, maxPrefetchCount);
        _producerLimiterSemaphore = new SemaphoreSlim(finalMaxConcurrentProducerCount, finalMaxConcurrentProducerCount);
        _prefetchLimiterSemaphore = new SemaphoreSlim(maxPrefetchCount, maxPrefetchCount);

        _channel = Channel.CreateUnbounded<Chunk<TEntity>>();
    }

    /// <summary>
    ///     Asynchronously loads entities in chunks.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>An asynchronous enumerable of chunks.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public IAsyncEnumerable<Chunk<TEntity>> LoadAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _isUsed, 1) == 1)
        {
            throw new InvalidOperationException("This loader instance has already been used. Please create a new instance for each load operation.");
        }

        return LoadCoreAsync(cancellationToken);

        async IAsyncEnumerable<Chunk<TEntity>> LoadCoreAsync([EnumeratorCancellation] CancellationToken ct)
        {
            var producersTask = StartProducersAsync(ct);
            var channelReader = CreateChannelReader();

            await foreach (var chunk in channelReader.ReadAsync(ct))
            {
                StatisticsMonitor?.DecrementQueueSize();
                _prefetchLimiterSemaphore.Release();
                yield return chunk;
            }

            await producersTask;
        }
    }

    /// <summary>
    ///     Disposes resources used by the loader.
    /// </summary>
    public void Dispose()
    {
        _producerLimiterSemaphore.Dispose();
        _prefetchLimiterSemaphore.Dispose();
    }

    private async Task StartProducersAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();

        try
        {
            var entityCount = await GetExpectedEntityCountAsync();
            var chunkCount = (int) (entityCount / _chunkSize) + (entityCount % _chunkSize > 0 ? 1 : 0);

            _logger?.LogTrace("Starting chunked entity loader for EntityTypeName={EntityTypeName} with ChunkSize={ChunkSize}, MaxConcurrentProducerCount={MaxConcurrentProducerCount}, MaxPrefetchCount={MaxPrefetchCount}, ExpectedEntityCount={ExpectedEntityCount}, ChunkCount={ChunkCount}.",
                EntityTypeName, _chunkSize, _producerLimiterSemaphore.CurrentCount, _prefetchLimiterSemaphore.CurrentCount, entityCount, chunkCount);

            var tasks = new List<Task>(chunkCount);

            for (var i = 0; i < chunkCount && !HasErrors; i++)
            {
                var currentChunkIndex = i;

                await _prefetchLimiterSemaphore.WaitAsync(cancellationToken);
                await _producerLimiterSemaphore.WaitAsync(cancellationToken);

                var task = Task.Run(() => ProduceAndReleaseSemaphoreAsync(currentChunkIndex, cancellationToken), cancellationToken); // we do not await this task to allow concurrent production
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
            _channel.Writer.TryComplete();
        }
        catch (Exception ex)
        {
            HasErrors = true;
            _logger?.LogError(ex, "Exception in StartProducersAsync.");
            _channel.Writer.TryComplete(ex);
        }
    }

    private async Task ProduceAndReleaseSemaphoreAsync(int chunkIndex, CancellationToken cancellationToken)
    {
        try
        {
            StatisticsMonitor?.IncrementActiveProducer();
            await ProduceAsync(chunkIndex, cancellationToken);
        }
        finally
        {
            StatisticsMonitor?.DecrementActiveProducer();
            _producerLimiterSemaphore.Release();
        }
    }

    private async Task ProduceAsync(int chunkIndex, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = _dbContextFactory();
            var query = _sourceQueryProvider(context);
            var startIndex = chunkIndex * _chunkSize;

            _logger?.LogTrace("Producing chunk #{ChunkIndex} with StartIndex={StartIndex} for EntityTypeName={EntityTypeName}", chunkIndex, startIndex, EntityTypeName);

            var startedTimestamp = Stopwatch.GetTimestamp();

            if (ChunkProductionStarted is not null)
            {
                await ChunkProductionStarted.Invoke(chunkIndex);
            }

            var entities = await query
                                .Skip(startIndex)
                                .Take(_chunkSize)
                                .ToListAsync(cancellationToken);

            _logger?.LogTrace("Produced chunk #{ChunkIndex} with StartIndex={StartIndex} for EntityTypeName={EntityTypeName} with {EntityCount} entities in {DurationInMs} ms.", chunkIndex, startIndex, EntityTypeName, entities.Count, (int) Stopwatch.GetElapsedTime(startedTimestamp).TotalMilliseconds);

            var chunk = new Chunk<TEntity>(chunkIndex, entities);
            await _channel.Writer.WriteAsync(chunk, cancellationToken);
            StatisticsMonitor?.IncrementQueueSize();
        }
        catch (Exception ex)
        {
            HasErrors = true;
            _prefetchLimiterSemaphore.Release(); // required, otherwise LoadAsync() would hang indefinitely
            _logger?.LogError(ex, "Error producing chunk #{ChunkIndex} for EntityTypeName={EntityTypeName}.", chunkIndex, EntityTypeName);
            throw;
        }
    }

    private IChannelReader<TEntity> CreateChannelReader()
        => _options.HasFlag(ChunkedEntityLoaderOptions.PreserveChunkOrder)
            ? new OrderedChannelReader<TEntity>(_channel.Reader, _loggerFactory?.CreateLogger<OrderedChannelReader<TEntity>>())
            : new UnorderedChannelReader<TEntity>(_channel.Reader, _loggerFactory?.CreateLogger<UnorderedChannelReader<TEntity>>());

    private async Task<long> GetExpectedEntityCountAsync()
    {
        await using var context = _dbContextFactory();

        _logger?.LogTrace("Getting total entity expected entity count for EntityTypeName={EntityTypeName}", EntityTypeName);
        var count = await _sourceQueryProvider(context).LongCountAsync();

        _logger?.LogTrace("Expected entity count for EntityTypeName={EntityTypeName} is {EntityCount}.", EntityTypeName, count);
        return count;
    }
}
