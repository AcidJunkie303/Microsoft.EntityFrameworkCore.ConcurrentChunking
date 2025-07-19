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
    ///     Ensures that chunks are processed in the order they are retrieved.
    /// </summary>
    PreserveChunkOrder = 1
}
