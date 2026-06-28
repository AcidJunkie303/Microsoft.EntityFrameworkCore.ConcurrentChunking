namespace Microsoft.EntityFrameworkCore.ConcurrentChunking;

/// <summary>
///     Defines an interface for loading entities in chunks asynchronously.
/// </summary>
/// <typeparam name="TEntity">The type of the entity being loaded.</typeparam>
public interface IChunkedEntityLoader<TEntity>
    where TEntity : class
{
    /// <summary>
    ///     Loads entities asynchronously in chunks.
    /// </summary>
    /// <returns>An asynchronous enumerable of chunks containing entities.</returns>
    IAsyncEnumerable<Chunk<TEntity>> LoadAsync() => LoadAsync(CancellationToken.None);

    /// <summary>
    ///     Loads entities asynchronously in chunks, allowing cancellation of the operation.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An asynchronous enumerable of chunks containing entities.</returns>
    /// <exception cref="InvalidOperationException">Thrown when this method is called more than once on the same instance.</exception>
    IAsyncEnumerable<Chunk<TEntity>> LoadAsync(CancellationToken cancellationToken);
}
