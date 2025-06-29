namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.Tests;

internal sealed class MyDbContextFactory : IDbContextFactory<MyDbContext>
{
    public MyDbContext CreateDbContext() => new ();
}
