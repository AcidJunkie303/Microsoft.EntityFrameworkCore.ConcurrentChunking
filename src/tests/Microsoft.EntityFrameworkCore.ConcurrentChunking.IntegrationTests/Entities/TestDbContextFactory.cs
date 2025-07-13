namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.IntegrationTests.Entities;

internal sealed class TestDbContextFactory : IDbContextFactory<TestDbContext>
{
    public TestDbContext CreateDbContext() => new();
}
