using Microsoft.EntityFrameworkCore.ConcurrentChunking;
using Playground.Logging;

namespace Playground;

#pragma warning disable

internal static class Program
{
    private static async Task Main()
    {
        InitializeDbContext();

        await DocSample1();

        return;

        // do your test stuff here
        await using var ctx = new TestDbContext();

        var entities = await ctx.TestEntities
                                .Where(a => a.Id < 1000)
                                .OrderByDescending(a => a.Id)
                                .Select(a => new { Bla = a.Id, a.Value })
                                .LoadChunkedAsync
                                 (
                                     dbContextFactory: () => new TestDbContext(),
                                     chunkSize: 10,
                                     maxDegreeOfParallelism: 10,
                                     options: ChunkedEntityLoaderOptions.PreserveChunkOrder
                                 )
                                .ToListAsync();
    }

    private static void InitializeDbContext()
    {
        const int entityCount = 100_001;

        using var ctx = new TestDbContext();

        if (ctx.TestEntities.Any())
        {
            return;
        }

        for (var i = 0; i < entityCount; i++)
        {
            var entity = new TestEntity
            {
                Id = i,
                Value = $"Entity {i}"
            };
            ctx.TestEntities.Add(entity);
        }

        ctx.SaveChanges();
    }

    private static async Task DocSample1()
    {
        await using var ctx = new TestDbContext();

        var chunks = ctx.TestEntities
                        .OrderByDescending(a => a.Id)
                        .LoadChunkedAsync
                         (
                             dbContextFactory: () => new TestDbContext(),
                             chunkSize: 1_000,
                             maxDegreeOfParallelism: 10,
                             options: ChunkedEntityLoaderOptions.PreserveChunkOrder,
                             loggerFactory: new ConsoleLoggerFactory()
                         );

        await Task.Delay(10_000);

        await foreach (var chunk in chunks)
        {
            /*
            foreach (var entity in chunk.Entities)
            {
                Console.WriteLine($"Id: {entity.Id}, Value: {entity.Value}");
            }
            */
        }
    }
}
