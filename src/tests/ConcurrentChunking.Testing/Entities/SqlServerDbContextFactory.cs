namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Entities;

public sealed class SqlServerDbContextFactory : IDbContextFactory<SqlServerDbContext>
{
    public SqlServerDbContext CreateDbContext() => new();
}
