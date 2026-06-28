namespace Microsoft.EntityFrameworkCore.ConcurrentChunking;

internal sealed class CompletedChunkListReader<T> : ICompletedChunkListReader<T>
{
    private readonly CompletedChunkList<T> _completedChunkList;

    public CompletedChunkListReader(CompletedChunkList<T> completedChunkList)
    {
        _completedChunkList = completedChunkList;
    }

    public Chunk<T>? TryGetAndRemoveNextChunk() => _completedChunkList.TryGetAndRemoveAnyChunk();
}
