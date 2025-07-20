namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.Linq;

internal sealed class DbContextFactoryForFunc<TDbContext> : IDbContextFactory<TDbContext>
    where TDbContext : DbContext
{
    private readonly Func<TDbContext> _dbContextFactory;

    public DbContextFactoryForFunc(Func<TDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public TDbContext CreateDbContext() => _dbContextFactory();
}
