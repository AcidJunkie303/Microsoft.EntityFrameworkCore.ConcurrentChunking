namespace Microsoft.EntityFrameworkCore.ConcurrentChunking;

/// <summary>
///     Callback arguments for the end of a chunk production.
/// </summary>
/// <typeparam name="TContext">The type of the database context.</typeparam>
public interface ICallbackArgs<TContext>
    where TContext : DbContext
{
    /// <summary>
    ///     The <see cref="DbContext" />.
    /// </summary>
    TContext DbContext { get; set; }

    /// <summary>
    ///     The index of the chunk.
    /// </summary>
    int ChunkIndex { get; }

    /// <summary>
    ///     The state, which can be used to carry data from the start callback to the end callback.
    /// </summary>
    object? State { get; set; }
}
