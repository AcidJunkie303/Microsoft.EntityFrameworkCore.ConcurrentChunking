namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.Tests.Entities;

internal sealed class TestDbContextFactory : IDbContextFactory<TestDbContext>
{
    public TestDbContext CreateDbContext() => new();
}
