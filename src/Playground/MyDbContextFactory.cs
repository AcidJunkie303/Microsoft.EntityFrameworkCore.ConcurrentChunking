using Microsoft.EntityFrameworkCore;

namespace Playground;

internal sealed class MyDbContextFactory : IDbContextFactory<MyDbContext>
{
    public MyDbContext CreateDbContext() => new();
}
