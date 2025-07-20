namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Entities;

public sealed class InMemoryDbContextFactory : IDbContextFactory<InMemoryDbContext>
{
    public InMemoryDbContext CreateDbContext() => new();
}
