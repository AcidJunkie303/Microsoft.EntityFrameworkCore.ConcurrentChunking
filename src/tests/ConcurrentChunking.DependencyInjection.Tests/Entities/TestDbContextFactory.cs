namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.DependencyInjection.Tests.Entities;

internal sealed class TestDbContextFactory : IDbContextFactory<TestDbContext>
{
    public TestDbContext CreateDbContext() => new();
}
