namespace Microsoft.EntityFrameworkCore.ConcurrentChunking;

internal sealed record EndCallbackArgs<TContext>(TContext DbContext, int ChunkIndex, object? State) : IEndCallbackArgs<TContext>
    where TContext : DbContext;
