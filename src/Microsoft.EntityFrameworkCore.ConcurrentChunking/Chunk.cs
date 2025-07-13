namespace Microsoft.EntityFrameworkCore.ConcurrentChunking;

public sealed record Chunk<TEntity>(int ChunkIndex, IReadOnlyList<TEntity> Entities)
{
    internal bool IsTerminatingChunk { get; init; }
}
