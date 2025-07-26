namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Data;

public interface ITestData
{
    static abstract int EntityCount { get; }
}

public interface ITestData<TDbContext> : ITestData
    where TDbContext : DbContext
{
    public static abstract ITestData<TDbContext> Instance { get; }
    Task EnsureInitializedAsync();
    IDbContextFactory<TDbContext> GetDbContextFactory();
    TDbContext CreateDbContext();
}
