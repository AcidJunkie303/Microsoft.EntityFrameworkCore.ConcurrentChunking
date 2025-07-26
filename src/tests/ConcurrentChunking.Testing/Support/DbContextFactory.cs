using Microsoft.EntityFrameworkCore;

namespace ConcurrentChunking.Testing.Support;

public sealed class DbContextFactory<TDbContext> : IDbContextFactory<TDbContext>
    where TDbContext : DbContext
{
    private readonly Func<TDbContext> _factory;

    public DbContextFactory(Func<TDbContext> factory)
    {
        _factory = factory;
    }

    public TDbContext CreateDbContext() => _factory();
}
