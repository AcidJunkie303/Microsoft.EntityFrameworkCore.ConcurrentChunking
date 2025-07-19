using Microsoft.EntityFrameworkCore.ConcurrentChunking;
using Microsoft.EntityFrameworkCore.ConcurrentChunking.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Playground;

internal static class ExamplesForDocumentation
{
    public static async Task ChunkedEntityLoaderDemoAsync()
    {
        using var loader = new ChunkedEntityLoader<TestDbContext, SimpleEntity>(
            dbContextFactory: () => new TestDbContext(),
            chunkSize: 100_000,
            maxConcurrentProducerCount: 3,
            maxPrefetchCount: 5,
            sourceQueryProvider: ctx => ctx.SimpleEntities.OrderBy(e => e.Id),
            options: ChunkedEntityLoaderOptions.PreserveChunkOrder
        );

        await foreach (var chunk in loader.LoadAsync(CancellationToken.None))
        {
            foreach (var entity in chunk.Entities)
            {
                Console.WriteLine($"Entity ID: {entity.Id}");
            }
        }
    }

    public static async Task DiDemoAsync(IServiceProvider services)
    {
var factory = services.GetRequiredService<IChunkedEntityLoaderFactory<TestDbContext>>();

using var loader = factory.Create(
    chunkSize: 100_000,
    maxConcurrentProducerCount: 3,
    maxPrefetchCount: 5,
    sourceQueryProvider: ctx => ctx.SimpleEntities.OrderBy(e => e.Id),
    options: ChunkedEntityLoaderOptions.PreserveChunkOrder
);

await foreach (var chunk in loader.LoadAsync(CancellationToken.None))
{
    foreach (var entity in chunk.Entities)
    {
        Console.WriteLine($"Entity ID: {entity.Id}");
    }
}
    }
}
