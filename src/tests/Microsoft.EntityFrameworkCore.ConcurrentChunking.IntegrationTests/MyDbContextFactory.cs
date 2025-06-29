namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.IntegrationTests;

internal sealed class MyDbContextFactory : IDbContextFactory<MyDbContext>
{
    public MyDbContext CreateDbContext() => new();
}
