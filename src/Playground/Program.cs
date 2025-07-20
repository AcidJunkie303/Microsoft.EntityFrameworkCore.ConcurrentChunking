using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ConcurrentChunking;
using Microsoft.EntityFrameworkCore.ConcurrentChunking.Linq;
using Playground.Logging;

namespace Playground;

#pragma warning disable

internal static class Program
{
    private static async Task Main()
    {
        using var consoleLoggerFactory = new ConsoleLoggerFactory();
        await using var ctx = new SqlServerDbContext();

        try
        {
            var chunks = await ctx.SimpleEntities
                                  .OrderBy(a => a.Id)
                                  .AsNoTracking()
                                  .LoadChunkedAsync(
                                       () => new SqlServerDbContext(),
                                       chunkSize: 100_000,
                                       maxConcurrentProducerCount: 5,
                                       maxPrefetchCount: 5,
                                       options: ChunkedEntityLoaderOptions.PreserveChunkOrder,
                                       loggerFactory: consoleLoggerFactory
                                   )
                                  .ToListAsync();

            Console.WriteLine($"Retrieved {chunks.Count} chunks with total {chunks.Sum(a => a.Entities.Count)} entities.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        /*
        InitializeDbContext();

        await DocSample1();

        return;

        // do your test stuff here
        await using var ctx = new TestDbContext();

        var entities = await ctx.SimpleEntities
                                .Where(a => a.Id < 1000)
                                .OrderByDescending(a => a.Id)
                                .Select(a => new { Bla = a.Id, a.Value })
                                .LoadChunkedAsync
                                 (
                                     dbContextFactory: () => new TestDbContext(),
                                     chunkSize: 10,
                                     maxConcurrentProducerCount: 5,
                                     maxPrefetchCount: 10,
                                     options: ChunkedEntityLoaderOptions.PreserveChunkOrder
                                 )
                                .ToListAsync();
                                */
    }

    private static async Task DocSample1()
    {
        await using var ctx = new SqlServerDbContext();

        var chunks = ctx.SimpleEntities
                        .OrderByDescending(a => a.Value2)
                        .LoadChunkedAsync
                         (
                             dbContextFactory: () =>
                             {
                                 var db = new SqlServerDbContext();
                                 db.Database.SetCommandTimeout(3);
                                 return db;
                             },
                             chunkSize: 1_000,
                             maxConcurrentProducerCount: 5,
                             maxPrefetchCount: 10,
                             options: ChunkedEntityLoaderOptions.PreserveChunkOrder,
                             loggerFactory: null
                         );

        await foreach (var chunk in chunks)
        {
            foreach (var entity in chunk.Entities)
            {
                // do something here
            }
        }
    }
}
