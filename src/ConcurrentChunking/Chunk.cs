using System.Diagnostics.CodeAnalysis;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking;

/// <summary>
///     Represents a chunk of entities with an associated index. Used for concurrent chunking operations.
/// </summary>
/// <typeparam name="TEntity">The type of the entities contained in the chunk.</typeparam>
/// <param name="ChunkIndex">The zero-based index of the chunk.</param>
/// <param name="Entities">The entity type.</param>
[SuppressMessage("Design", "CA1000:Do not declare static members on generic types")]
public sealed record Chunk<TEntity>(int ChunkIndex, IReadOnlyList<TEntity> Entities)
{
    /// <summary>
    ///     Gets a special terminating chunk instance, used to signal the end of chunk processing.
    /// </summary>
    public static Chunk<TEntity> TerminatingChunk { get; } = new();

    internal bool IsTerminatingChunk { get; }

    private Chunk() : this(0, [])
    {
        IsTerminatingChunk = true;
    }
}
