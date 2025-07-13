using Microsoft.EntityFrameworkCore.ConcurrentChunking.Tests.Entities;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.Tests;

internal static class TestData
{
    public const int EntityCount = 10001;
    public const int DefaultChunkSize = 1000;

    private static bool IsInitialized;
    private static readonly object Lock = new();

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
        using var ctx = new TestDbContext();

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
