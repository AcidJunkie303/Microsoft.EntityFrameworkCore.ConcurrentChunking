# Microsoft.EntityFrameworkCore.ConcurrentChunking

If retrieving large collections of entities from Entity Framework is taking a long time,
this library enables parallel and pre-emptive loading of entities, resulting in much faster data access over time.

# Examples

## Basic Usage Of ChunkedEntityLoader&lt;TDbContext, TEntity&gt;

Required packages:

- Microsoft.EntityFrameworkCore.ConcurrentChunking

### Example:

```csharp
using var loader = new ChunkedEntityLoader<TestDbContext, SimpleEntity>(
    dbContextFactory: () => new TestDbContext(),
    chunkSize: 100_000,
    maxConcurrentProducerCount: 3,
    maxPrefetchCount: 5,
    sourceQueryProvider: ctx => ctx.SimpleEntities
        .Include(e => e.RelatedEntity).ThenInclude(e => e.AnotherRelatedEntity)
        .Include(e => e.RelatedEntity2)
        .OrderBy(e => e.Id),
    options: ChunkedEntityLoaderOptions.PreserveChunkOrder
);

await foreach (var chunk in  loader.LoadAsync(CancellationToken.None))
{
    foreach (var entity in chunk.Entities)
    {
        Console.WriteLine($"Entity ID: {entity.Id}");
    }
}
```

## Basic Usage From IQueryable&lt;T&gt;

Required packages:

- Microsoft.EntityFrameworkCore.ConcurrentChunking
- Microsoft.EntityFrameworkCore.ConcurrentChunking.Linq

### Example:

```csharp
await using var ctx = new TestDbContext();

var chunks = ctx.TestEntities
    .Include(e => e.RelatedEntity).ThenInclude(e => e.AnotherRelatedEntity)
    .Include(e => e.RelatedEntity2)
    .AsNoTracking()
    .OrderByDescending(a => a.Id)
    .LoadChunkedAsync(
        dbContextFactory: () => new TestDbContext(),
        chunkSize: 100_000,
        maxConcurrentProducerCount: 3,
        maxPrefetchCount: 5,
        options: ChunkedEntityLoaderOptions.PreserveChunkOrder);

await foreach (var chunk in chunks)
{
    foreach(var entity in chunk.Entities)
    {
        Console.WriteLine($"Entity ID: {entity.Id}");
    }
}
```

## Basic Usage From IChunkedEntityLoaderFactory&lt;TDbContext&gt; through Dependency Injection

Required packages:

- Microsoft.EntityFrameworkCore.ConcurrentChunking
- Microsoft.EntityFrameworkCore.ConcurrentChunking.DependencyInjection

### Example:

Make sure to register `IChunkedEntityLoaderFactory<TDbContext>` in your DI container:

```csharp
services.AddChunkedEntityLoaderFactory();
````

```csharp
var factory = services.GetRequiredService<IChunkedEntityLoaderFactory<TestDbContext>>();

using var loader = factory.Create(
    chunkSize: 100_000,
    maxConcurrentProducerCount: 3,
    maxPrefetchCount: 5,
    sourceQueryProvider: ctx => ctx.SimpleEntities
        .Include(e => e.RelatedEntity).ThenInclude(e => e.AnotherRelatedEntity)
        .Include(e => e.RelatedEntity2)
        .OrderBy(e => e.Id),
    options: ChunkedEntityLoaderOptions.PreserveChunkOrder
);

await foreach (var chunk in loader.LoadAsync(CancellationToken.None))
{
    foreach (var entity in chunk.Entities)
    {
        Console.WriteLine($"Entity ID: {entity.Id}");
    }
}
```
