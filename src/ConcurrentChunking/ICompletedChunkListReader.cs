namespace Microsoft.EntityFrameworkCore.ConcurrentChunking;

internal interface ICompletedChunkListReader<T>
{
    Chunk<T>? TryGetAndRemoveNextChunk();
}
