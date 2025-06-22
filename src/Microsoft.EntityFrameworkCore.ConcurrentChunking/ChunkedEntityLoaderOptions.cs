namespace Microsoft.EntityFrameworkCore.ConcurrentChunking;

[Flags]
public enum ChunkedEntityLoaderOptions
{
    None = 0,
    PreserveChunkOrder= 1
}
