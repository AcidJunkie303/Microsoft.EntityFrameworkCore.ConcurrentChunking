namespace Microsoft.EntityFrameworkCore.ConcurrentChunking;

/// <summary>
///     Provides behavioral options for the <see cref="ChunkedEntityLoader{TDbContext,TEntity}" />.
/// </summary>
[Flags]
public enum ChunkedEntityLoaderOptions
{
    /// <summary>
    ///     No special options are applied.
    /// </summary>
    None = 0,

    /// <summary>
    ///     Ensures that chunks are yielded to the consumer in ascending chunk index order,
    ///     regardless of the order in which they are retrieved from the database.
    /// </summary>
    PreserveChunkOrder = 1
}
