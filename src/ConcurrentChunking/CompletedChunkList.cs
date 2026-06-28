namespace Microsoft.EntityFrameworkCore.ConcurrentChunking;

internal sealed class CompletedChunkList<T>
{
    private readonly int _capacity;
    private readonly Dictionary<int, Chunk<T>> _chunksByIndex;

    public bool IsFull => _chunksByIndex.Count == _capacity;
    public int FreeSlotCount => _capacity - _chunksByIndex.Count;

    public CompletedChunkList(int capacity)
    {
        _capacity = capacity;
        _chunksByIndex = new Dictionary<int, Chunk<T>>(capacity);
    }

    public Chunk<T> GetAndRemoveChunk(int chunkIndex) => TryGetAndRemoveChunk(chunkIndex) ?? throw new ArgumentException($"Chunk #{chunkIndex} not found in chunk list.", nameof(chunkIndex));

    public Chunk<T>? TryGetAndRemoveAnyChunk()
    {
        var (index, chunk) = _chunksByIndex.FirstOrDefault();
        if (chunk is null)
        {
            return null;
        }

        _chunksByIndex.Remove(index);
        return chunk;
    }

    public Chunk<T>? TryGetAndRemoveChunk(int chunkIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(chunkIndex);

        _chunksByIndex.TryGetValue(chunkIndex, out var chunk);
        if (chunk is null)
        {
            return null;
        }

        _chunksByIndex.Remove(chunkIndex);
        return chunk;
    }

    public void AddChunk(Chunk<T> chunk)
    {
        var index = chunk.ChunkIndex;

        if (!_chunksByIndex.TryAdd(index, chunk))
        {
            throw new ArgumentException($"Chunk #{index} already exists in chunk list.", nameof(chunk));
        }
    }
}
