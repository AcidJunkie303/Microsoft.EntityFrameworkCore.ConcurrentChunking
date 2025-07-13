using System.Diagnostics.CodeAnalysis;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking;

[SuppressMessage("Design", "CA1000:Do not declare static members on generic types")]
public sealed record Chunk<TEntity>(int ChunkIndex, IReadOnlyList<TEntity> Entities)
{
    public static Chunk<TEntity> TerminatingChunk { get; } = new();

    internal bool IsTerminatingChunk { get; }

    private Chunk() : this(0, [])
    {
        IsTerminatingChunk = true;
    }
}
