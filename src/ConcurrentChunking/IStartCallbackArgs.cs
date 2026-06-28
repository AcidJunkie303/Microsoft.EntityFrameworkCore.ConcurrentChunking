namespace Microsoft.EntityFrameworkCore.ConcurrentChunking;

/// <summary>
///     Callback arguments for the start of chunk production
/// </summary>
/// <typeparam name="TContext">The type of the database context.</typeparam>
public interface IStartCallbackArgs<out TContext>
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
}
