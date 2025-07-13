namespace Microsoft.EntityFrameworkCore.ConcurrentChunking;

public interface IChunkedEntityLoader<TEntity> : IDisposable
    where TEntity : class
{
    IAsyncEnumerable<Chunk<TEntity>> LoadAsync() => LoadAsync(CancellationToken.None);
    IAsyncEnumerable<Chunk<TEntity>> LoadAsync(CancellationToken cancellationToken);
}
