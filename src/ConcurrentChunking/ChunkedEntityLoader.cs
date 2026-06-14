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
    private readonly int _maxConcurrentProducerCount;
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
    /// <param name="chunkSize">The size of each chunk. Must be at least 1.</param>
    /// <param name="maxConcurrentProducerCount">Maximum number of concurrent producers. Must be at least 1.</param>
    /// <param name="maxPrefetchCount">Maximum number of chunks to prefetch. Must be at least 1.</param>
    /// <param name="sourceQueryProvider">
    ///     Function to provide the ordered query for retrieving entities.
    ///     The ordering must be deterministic and use unique column(s) (single unique key or unique key combination)
    ///     because chunking relies on <c>Skip</c>/<c>Take</c> pagination.
    ///     It is the caller's responsibility to ensure the ordering includes unique columns.
    /// </param>
    /// <param name="options">Loader options.</param>
    /// <param name="loggerFactory">Optional logger factory.</param>
    /// <param name="logger">Optional logger.</param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="dbContextFactory" /> or
    ///     <paramref name="sourceQueryProvider" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when <paramref name="chunkSize" />,
    ///     <paramref name="maxConcurrentProducerCount" />, or <paramref name="maxPrefetchCount" /> is less than 1.
    /// </exception>
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
    /// <param name="chunkSize">The size of each chunk. Must be at least 1.</param>
    /// <param name="maxConcurrentProducerCount">Maximum number of concurrent producers. Must be at least 1.</param>
    /// <param name="maxPrefetchCount">Maximum number of chunks to prefetch. Must be at least 1.</param>
    /// <param name="sourceQueryProvider">
    ///     Function to provide the ordered query for retrieving entities.
    ///     The ordering must be deterministic and use unique column(s) (single unique key or unique key combination)
    ///     because chunking relies on <c>Skip</c>/<c>Take</c> pagination.
    ///     It is the caller's responsibility to ensure the ordering includes unique columns.
    /// </param>
    /// <param name="options">Loader options.</param>
    /// <param name="loggerFactory">Optional logger factory.</param>
    /// <param name="logger">Optional logger.</param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="dbContextFactory" /> or
    ///     <paramref name="sourceQueryProvider" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when <paramref name="chunkSize" />,
    ///     <paramref name="maxConcurrentProducerCount" />, or <paramref name="maxPrefetchCount" /> is less than 1.
    /// </exception>
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
        _maxConcurrentProducerCount = finalMaxConcurrentProducerCount;
        _producerLimiterSemaphore = new SemaphoreSlim(finalMaxConcurrentProducerCount, finalMaxConcurrentProducerCount);
        _prefetchLimiterSemaphore = new SemaphoreSlim(maxPrefetchCount, maxPrefetchCount);

        _channel = Channel.CreateUnbounded<Chunk<TEntity>>();
    }

    /// <summary>
    ///     Asynchronously loads entities in chunks.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>An asynchronous enumerable of chunks.</returns>
    /// <exception cref="InvalidOperationException">Thrown when this method is called more than once on the same instance.</exception>
    public IAsyncEnumerable<Chunk<TEntity>> LoadAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _isUsed, 1) == 1)
        {
            throw new InvalidOperationException("This loader instance has already been used. Please create a new instance for each load operation.");
        }

        return LoadCoreAsync(cancellationToken);

        async IAsyncEnumerable<Chunk<TEntity>> LoadCoreAsync([EnumeratorCancellation] CancellationToken ct)
        {
            using var producerCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var producersTask = StartProducersAsync(producerCancellationTokenSource.Token);
            var channelReader = CreateChannelReader();

            try
            {
                await foreach (var chunk in channelReader.ReadAsync(ct))
                {
                    StatisticsMonitor?.DecrementQueueSize();
                    _prefetchLimiterSemaphore.Release();
                    yield return chunk;
                }
            }
            finally
            {
                if (!producersTask.IsCompleted)
                {
                    await producerCancellationTokenSource.CancelAsync();
                }

                try
                {
                    await producersTask;
                }
                catch (OperationCanceledException) when (producerCancellationTokenSource.IsCancellationRequested)
                {
                    // Cancellation is expected when the consumer stops enumeration early.
                }
            }
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

    private static async Task RemoveCompletedTasksIfNecessaryAsync(List<Task> tasks, int maxConcurrentProducerCount, bool force)
    {
        // we only clean-up when forced or when we have a certain amount of tasks in the list
        var shouldCleanUp = tasks.Count >= maxConcurrentProducerCount * 2;
        if (!(shouldCleanUp || force))
        {
            return;
        }

        for (var i = tasks.Count - 1; i >= 0; i--)
        {
            var task = tasks[i];
            if (!task.IsCompleted)
            {
                continue;
            }

            await task;
            tasks.RemoveAt(i);
        }
    }

    private async Task StartProducersAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();

        try
        {
            var entityCount = await GetExpectedEntityCountAsync(cancellationToken);
            var chunkCount = CalculateChunkCount(entityCount);

            _logger?.LogTrace("Starting chunked entity loader for EntityTypeName={EntityTypeName} with ChunkSize={ChunkSize}, MaxConcurrentProducerCount={MaxConcurrentProducerCount}, MaxPrefetchCount={MaxPrefetchCount}, ExpectedEntityCount={ExpectedEntityCount}, ChunkCount={ChunkCount}.",
                EntityTypeName, _chunkSize, _producerLimiterSemaphore.CurrentCount, _prefetchLimiterSemaphore.CurrentCount, entityCount, chunkCount);

            var tasks = new List<Task>(_maxConcurrentProducerCount * 3);

            for (var i = 0; i < chunkCount && !HasErrors; i++)
            {
                var currentChunkIndex = i;

                await _prefetchLimiterSemaphore.WaitAsync(cancellationToken);
                await _producerLimiterSemaphore.WaitAsync(cancellationToken);

                var task = Task.Run(() => ProduceAndReleaseSemaphoreAsync(currentChunkIndex, cancellationToken), CancellationToken.None); // we do not await this task to allow concurrent production
                tasks.Add(task);

                await RemoveCompletedTasksIfNecessaryAsync(tasks, _maxConcurrentProducerCount, force: false);
            }

            await Task.WhenAll(tasks);
            _channel.Writer.TryComplete();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
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
            var startIndex = checked(chunkIndex * _chunkSize);

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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _prefetchLimiterSemaphore.Release(); // required, otherwise StartProducersAsync() could remain blocked
            throw;
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

    private async Task<long> GetExpectedEntityCountAsync(CancellationToken cancellationToken)
    {
        await using var context = _dbContextFactory();

        _logger?.LogTrace("Getting total expected entity count for EntityTypeName={EntityTypeName}", EntityTypeName);
        var count = await _sourceQueryProvider(context).LongCountAsync(cancellationToken);
        _logger?.LogTrace("Expected entity count for EntityTypeName={EntityTypeName} is {EntityCount}.", EntityTypeName, count);

        return count;
    }

    private int CalculateChunkCount(long entityCount)
    {
        var chunkCountLong = (entityCount / _chunkSize) + (entityCount % _chunkSize > 0 ? 1 : 0);

        if (chunkCountLong > int.MaxValue)
        {
            throw new InvalidOperationException($"The query for '{EntityTypeName}' produces too many chunks ({chunkCountLong}). The current implementation supports up to {int.MaxValue} chunks.");
        }

        if (chunkCountLong == 0)
        {
            return 0;
        }

        var maxStartIndex = (chunkCountLong - 1) * _chunkSize;
        if (maxStartIndex > int.MaxValue)
        {
            throw new InvalidOperationException($"The query for '{EntityTypeName}' contains too many rows for Skip/Take paging. Maximum supported start index is {int.MaxValue}, but calculated value is {maxStartIndex}.");
        }

        return (int) chunkCountLong;
    }
}
