# Microsoft.EntityFrameworkCore.ConcurrentChunking

If retrieving large collections of entities from Entity Framework is causing performance issues,
this library enables parallel and pre-emptive loading of entities, resulting in much faster data access over time.

# Examples

## Basic Usage From IQueryable&lt;T&gt;

```csharp
await using var ctx = new TestDbContext();

var chunks = ctx.TestEntities
    .OrderByDescending(a => a.Id)
    .LoadChunkedAsync(
        dbContextFactory: () => new TestDbContext(),
        chunkSize: 1_000,
        maxDegreeOfParallelism: 5,
        maxPrefetchCount: 10,
        options: ChunkedEntityLoaderOptions.PreserveChunkOrder,
        loggerFactory: new ConsoleLoggerFactory());

await foreach (var chunk in chunks)
{
    foreach(var entity in chunk.Entities)
    {
        // do domething here
    }
}
```
