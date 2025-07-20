namespace Microsoft.EntityFrameworkCore.ConcurrentChunking;

internal interface IChannelReader<TEntity>
{
    IAsyncEnumerable<Chunk<TEntity>> ReadAsync(CancellationToken cancellationToken);
}
