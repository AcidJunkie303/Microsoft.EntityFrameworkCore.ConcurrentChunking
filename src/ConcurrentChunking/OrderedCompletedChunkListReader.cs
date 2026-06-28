namespace Microsoft.EntityFrameworkCore.ConcurrentChunking;

internal sealed class OrderedCompletedChunkListReader<T> : ICompletedChunkListReader<T>
{
    private readonly CompletedChunkList<T> _completedChunkList;
    private int _currentChunkIndex;

    public OrderedCompletedChunkListReader(CompletedChunkList<T> completedChunkList)
    {
        _completedChunkList = completedChunkList;
    }

    public Chunk<T>? TryGetAndRemoveNextChunk()
    {
        var chunk = _completedChunkList.TryGetAndRemoveChunk(_currentChunkIndex);
        if (chunk is null)
        {
            return null;
        }

        _currentChunkIndex++;
        return chunk;
    }
}
