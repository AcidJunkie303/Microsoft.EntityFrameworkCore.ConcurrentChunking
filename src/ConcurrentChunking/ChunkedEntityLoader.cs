using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
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
    private readonly Func<TDbContext> _dbContextFactory;
    private readonly int _maxConcurrentProducerCount;
    private readonly int _chunkSize;
    private readonly int _maxPrefetchCount;
    private readonly Func<ICallbackArgs<TDbContext>, Task>? _startProducingChunkCallback;
    private readonly Func<ICallbackArgs<TDbContext>, Task>? _endProducingChunkCallback;
    private int _usageCounter;
    private int _emptyChunkCounter;

    /// <summary>
    ///     Used internally to do callbacks to the caller. This is used to simulate failures during unit tests.
    /// </summary>
    internal CallbackWhenKind? CallbackWhenKind { get; set; }

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
    /// <param name="startProducingChunkCallback">Callback for when a chunk is being produced.</param>
    /// <param name="endProducingChunkCallback">Callback for when a chunk was produced.</param>
    /// <param name="options">Loader options.</param>
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
        Func<ICallbackArgs<TDbContext>, Task>? startProducingChunkCallback = null,
        Func<ICallbackArgs<TDbContext>, Task>? endProducingChunkCallback = null,
        ChunkedEntityLoaderOptions options = ChunkedEntityLoaderOptions.PreserveChunkOrder,
        ILogger<ChunkedEntityLoader<TDbContext, TEntity>>? logger = null
    )
        : this
        (
            dbContextFactory: dbContextFactory.CreateDbContext,
            chunkSize: chunkSize,
            maxConcurrentProducerCount: maxConcurrentProducerCount,
            maxPrefetchCount: maxPrefetchCount,
            sourceQueryProvider: sourceQueryProvider,
            startProducingChunkCallback: startProducingChunkCallback,
            endProducingChunkCallback: endProducingChunkCallback, options: options,
            logger: logger
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
    /// <param name="startProducingChunkCallback">Callback for when a chunk is being produced.</param>
    /// <param name="endProducingChunkCallback">Callback for when a chunk was produced.</param>
    /// <param name="options">Loader options.</param>
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
        Func<ICallbackArgs<TDbContext>, Task>? startProducingChunkCallback = null,
        Func<ICallbackArgs<TDbContext>, Task>? endProducingChunkCallback = null,
        ChunkedEntityLoaderOptions options = ChunkedEntityLoaderOptions.PreserveChunkOrder,
        ILogger<ChunkedEntityLoader<TDbContext, TEntity>>? logger = null
    )
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);
        ArgumentOutOfRangeException.ThrowIfLessThan(chunkSize, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxConcurrentProducerCount, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxPrefetchCount, 1);
        ArgumentNullException.ThrowIfNull(sourceQueryProvider);

        _sourceQueryProvider = sourceQueryProvider;
        _startProducingChunkCallback = startProducingChunkCallback;
        _endProducingChunkCallback = endProducingChunkCallback;
        _options = options;
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _chunkSize = chunkSize;
        _maxPrefetchCount = maxPrefetchCount;
        _maxConcurrentProducerCount = Math.Min(maxConcurrentProducerCount, maxPrefetchCount);
    }

    /// <summary>
    ///     Asynchronously loads entities in chunks.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>An asynchronous enumerable of chunks.</returns>
    /// <exception cref="InvalidOperationException">Thrown when this method is called more than once on the same instance.</exception>
    public IAsyncEnumerable<Chunk<TEntity>> LoadAsync(CancellationToken cancellationToken)
    {
        AssertSingleUsage();

        return LoadCoreAsync(cancellationToken);
    }

    [SuppressMessage("Minor Code Smell", "S1227:break statements should not be used except for switch cases")]
    [SuppressMessage("Critical Code Smell", "S3776:Cognitive Complexity of methods should not be too high")]
    private async IAsyncEnumerable<Chunk<TEntity>> LoadCoreAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var producerCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var completedChunks = new CompletedChunkList<TEntity>(_maxPrefetchCount);
        var producerTasks = new ProducerList<TEntity>(_maxConcurrentProducerCount);
        ICompletedChunkListReader<TEntity> completedChunksReader = _options.HasFlag(ChunkedEntityLoaderOptions.PreserveChunkOrder)
            ? new OrderedCompletedChunkListReader<TEntity>(completedChunks)
            : new CompletedChunkListReader<TEntity>(completedChunks);

        var chunkIndex = 0;

        while (true)
        {
            await RemoveCompletedTasksFromProducerListAsync();

            if (HasEncounteredEmptyChunk())
            {
                break;
            }

            // return completed chunks
            while (true)
            {
                var chunk = completedChunksReader.TryGetAndRemoveNextChunk();
                if (chunk is null)
                {
                    break;
                }

                yield return chunk;
            }

            // fill all producer slots but also make sure that we do not exceed the max prefetch count (completedChunks)
            while (!producerTasks.IsFull && !completedChunks.IsFull)
            {
                var currentChunkIndex = chunkIndex;
                var producerTask = Task.Run(() => ProduceAsync(currentChunkIndex, cancellationToken), cancellationToken);
                producerTasks.Add(chunkIndex, producerTask);
                chunkIndex++;
            }

            await producerTasks.WhenAnyAsync(cancellationToken).ConfigureAwait(false);
        }

        // drain out all producers
        while (!producerTasks.IsEmpty)
        {
            var chunk = await producerTasks.GetAndRemoveNextCompletedChunkAsync(cancellationToken);
            if (chunk.Entities.Count != 0)
            {
                completedChunks.AddChunk(chunk);
            }
        }

        // retrieve the remaining chunks
        while (true)
        {
            var chunk = completedChunksReader.TryGetAndRemoveNextChunk();
            if (chunk is null)
            {
                break;
            }

            yield return chunk;
        }

        async Task RemoveCompletedTasksFromProducerListAsync()
        {
            while (true)
            {
                var chunk = await producerTasks.TryGetAndRemoveNextCompletedChunkAsync();
                if (chunk is null)
                {
                    return;
                }

                if (chunk.Entities.Count == 0)
                {
                    SetEmptyChunkEncountered();
                    return;
                }

                completedChunks.AddChunk(chunk);
            }
        }
    }

    private bool HasEncounteredEmptyChunk() => _emptyChunkCounter != 0;
    private void SetEmptyChunkEncountered() => Interlocked.Increment(ref _emptyChunkCounter);

    private void AssertSingleUsage()
    {
        var usageCount = Interlocked.Increment(ref _usageCounter);
        if (usageCount > 1)
        {
            throw new InvalidOperationException($"The {nameof(ChunkedEntityLoader<,>)} instance can only be used for a single enumeration. Current usage count: {usageCount}.");
        }
    }

    private async Task<Chunk<TEntity>> ProduceAsync(int chunkIndex, CancellationToken cancellationToken)
    {
        try
        {
            using var contextAndState = await CreateDbContextAndStateAsync(chunkIndex);

            try
            {
                var query = _sourceQueryProvider(contextAndState.DbContext);
                var startIndex = checked(chunkIndex * _chunkSize);

                _logger?.LogTrace("Producing chunk #{ChunkIndex} with StartIndex={StartIndex} for EntityTypeName={EntityTypeName}", chunkIndex, startIndex, EntityTypeName);

                var startedTimestamp = Stopwatch.GetTimestamp();

                var entities = await query
                                    .Skip(startIndex)
                                    .Take(_chunkSize)
                                    .ToListAsync(cancellationToken);

#pragma warning disable S125 // Commented out code
                // Use this to simulate random execution times
                //await Task.Delay(Random.Shared.Next(0, 5000));
#pragma warning restore S125

                _logger?.LogTrace("Produced chunk #{ChunkIndex} with StartIndex={StartIndex} for EntityTypeName={EntityTypeName} with {EntityCount} entities in {DurationInMs} ms.", chunkIndex, startIndex, EntityTypeName, entities.Count, (int) Stopwatch.GetElapsedTime(startedTimestamp).TotalMilliseconds);

                return new Chunk<TEntity>(chunkIndex, entities);
            }
            finally
            {
                if (_endProducingChunkCallback is not null)
                {
                    await _endProducingChunkCallback(new CallbackArgs<TDbContext>(contextAndState.DbContext, chunkIndex, contextAndState.State));
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error producing chunk #{ChunkIndex} for EntityTypeName={EntityTypeName}.", chunkIndex, EntityTypeName);
            throw;
        }
    }

    private async Task<DbContextAndState> CreateDbContextAndStateAsync(int chunkIndex)
    {
        TDbContext? dbContext = null;

        try
        {
            dbContext = _dbContextFactory();

            if (_startProducingChunkCallback is null)
            {
                return new DbContextAndState(dbContext, State: null, DisposeDbContext: true);
            }

            var args = new CallbackArgs<TDbContext>(dbContext, chunkIndex, state: null);
            await _startProducingChunkCallback(args);

            // If DbContext has been provided/overwritten by the callback
            var isOurDbContext = ReferenceEquals(dbContext, args.DbContext);
            if (!isOurDbContext)
            {
                await dbContext.DisposeAsync();
            }

            return new DbContextAndState(args.DbContext, args.State, isOurDbContext);
        }
        catch
        {
            if (dbContext is not null)
            {
                await dbContext.DisposeAsync();
            }

            throw;
        }
    }

    private sealed record DbContextAndState(TDbContext DbContext, object? State, bool DisposeDbContext) : IDisposable
    {
        public void Dispose()
        {
            if (DisposeDbContext)
            {
                DbContext.Dispose();
            }
        }
    }
}
