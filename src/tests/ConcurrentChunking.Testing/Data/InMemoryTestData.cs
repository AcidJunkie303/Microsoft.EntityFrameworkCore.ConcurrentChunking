using System.Diagnostics.CodeAnalysis;
using ConcurrentChunking.Testing.Support;
using Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Entities;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Data;

[SuppressMessage("Performance", "MA0158:Use System.Threading.Lock", Justification = "we support both, .NET8.0 and .NET9.0")]
public sealed class InMemoryTestData : TestData, ITestData<InMemoryDbContext>
{
    private readonly DbContextFactory<InMemoryDbContext> _dbContextFactory = new(() => new InMemoryDbContext());

    public static ITestData<InMemoryDbContext> Instance { get; } = new InMemoryTestData();
    public static int EntityCount => 100_001;
    public static int ChunkSize => 10_000;

    private InMemoryTestData()
    {
    }

    public IDbContextFactory<InMemoryDbContext> GetDbContextFactory() => _dbContextFactory;

    public InMemoryDbContext CreateDbContext() => _dbContextFactory.CreateDbContext();

    protected override async Task InitializeAsync()
    {
        await using var ctx = new InMemoryDbContext();

        if (await ctx.SimpleEntities.CountAsync() == EntityCount)
        {
            return;
        }

        ctx.SimpleEntities.RemoveRange(await ctx.SimpleEntities.ToListAsync());

        for (var i = 1; i <= EntityCount; i++)
        {
            var entity = new SimpleEntity(i, Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            ctx.SimpleEntities.Add(entity);
        }

        await ctx.SaveChangesAsync();
    }
}
