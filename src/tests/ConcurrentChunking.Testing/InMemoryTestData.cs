using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Entities;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing;

[SuppressMessage("Performance", "MA0158:Use System.Threading.Lock", Justification = "we support both, .NET8.0 and .NET9.0")]
public static class InMemoryTestData
{
    private static bool IsInitialized;
    private static readonly object Lock = new();
    public static int EntityCount { get; } = 1_000_001;

    public static void EnsureInitialized()
    {
        if (IsInitialized)
        {
            return;
        }

        lock (Lock)
        {
            if (IsInitialized)
            {
                return;
            }

            InitializeDbContext();
            IsInitialized = true;
        }
    }

    private static void InitializeDbContext()
    {
        using var ctx = new InMemoryDbContext();

        if (ctx.SimpleEntities.Count() == EntityCount)
        {
            return;
        }

        ctx.SimpleEntities.RemoveRange(ctx.SimpleEntities.ToList());

        for (var i = 1; i <= EntityCount; i++)
        {
            var entity = new SimpleEntity(i, Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            ctx.SimpleEntities.Add(entity);
        }

        ctx.SaveChanges();
    }
}
