using Microsoft.EntityFrameworkCore.ConcurrentChunking.Linq;
using PlaygroundWithNugetPackage.Logging;

namespace PlaygroundWithNugetPackage;

#pragma warning disable S106

internal static class Program
{
    private static async Task Main()
    {
        using var consoleLoggerFactory = new ConsoleLoggerFactory();
        await using var ctx = new SqlServerDbContext();

        var chunks = await ctx.SimpleEntities
                              .OrderBy(a => a.Id)
                              .LoadChunkedAsync(
                                   () => new SqlServerDbContext(),
                                   chunkSize: 100_000,
                                   maxConcurrentProducerCount: 5,
                                   maxPrefetchCount: 5,
                                   loggerFactory: consoleLoggerFactory
                               )
                              .ToListAsync();

        Console.WriteLine($"Retrieved {chunks.Count} chunks with total {chunks.Sum(a => a.Entities.Count)} entities.");
    }
}
