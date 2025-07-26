namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Data;

public interface ITestData<TDbContext> where TDbContext : DbContext
{
    public static abstract ITestData<TDbContext> Instance { get; }
    int EntityCount { get; }
    int ChunkSize { get; }

    Task EnsureInitializedAsync();
    IDbContextFactory<TDbContext> GetDbContextFactory();
    TDbContext CreateDbContext();
}
