namespace Microsoft.EntityFrameworkCore.ConcurrentChunking;

internal sealed record StartCallbackArgs<TContext>(TContext DbContext, int ChunkIndex) : IStartCallbackArgs<TContext>
    where TContext : DbContext;
