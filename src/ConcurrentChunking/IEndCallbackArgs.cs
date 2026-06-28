namespace Microsoft.EntityFrameworkCore.ConcurrentChunking;

/// <summary>
///     Callback arguments for the end of a chunk production.
/// </summary>
/// <typeparam name="TContext">The type of the database context.</typeparam>
public interface IEndCallbackArgs<out TContext>
    where TContext : DbContext
{
    /// <summary>
    ///     The <see cref="DbContext" />.
    /// </summary>
    TContext DbContext { get; }

    /// <summary>
    ///     The index of the chunk.
    /// </summary>
    int ChunkIndex { get; }

    /// <summary>
    ///     The state.
    /// </summary>
    object? State { get; }
}
