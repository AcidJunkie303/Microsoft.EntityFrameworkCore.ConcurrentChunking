using Microsoft.EntityFrameworkCore;

namespace Playground;

internal sealed class TestDbContextFactory : IDbContextFactory<TestDbContext>
{
    public TestDbContext CreateDbContext() => new();
}
