# Microsoft.EntityFrameworkCore.ConcurrentChunking

In scenarios where you need to process large entity sets, this library allows retrieving entities from entity framework
in a parallel and chunked manner.

# Examples

## Basic Usage From IQueryable&lt;T&gt;

```csharp
await using var ctx = new TestDbContext();
 
var chunks = ctx.TestEntities
    .Where(a => a.Id > 1000)
    .OrderByDescending(a => a.Id)
    .LoadChunkedAsync
    (
        dbContextFactory: () => new TestDbContext(),
        chunkSize: 25_000,
        maxDegreeOfParallelism: 10,
        options: ChunkedEntityLoaderOptions.PreserveChunkOrder
    );

await foreach (var chunk in chunks)
{
    foreach(var entity in chunk.Entities)
    {
        Console.WriteLine($"Id: {entity.Id}, Value: {entity.Value}");
    }
}
```

