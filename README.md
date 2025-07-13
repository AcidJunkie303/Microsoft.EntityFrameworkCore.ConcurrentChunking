# Microsoft.EntityFrameworkCore.ConcurrentChunking

In scenarios where you need to process large entity sets, this library allows retrieving entities from entity framework
in a parallel and chunked manner.

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

