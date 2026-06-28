namespace Microsoft.EntityFrameworkCore.ConcurrentChunking;

internal sealed class CallbackArgs<TContext> : ICallbackArgs<TContext>
    where TContext : DbContext
{
    public TContext DbContext { get; set; }
    public int ChunkIndex { get; }
    public object? State { get; set; }

    public CallbackArgs(TContext dbContext, int chunkIndex, object? state)
    {
        DbContext = dbContext;
        ChunkIndex = chunkIndex;
        State = state;
    }
}
