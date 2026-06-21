# EntityFrameworkCore.ConcurrentChunking

Concurrent chunk loading for EF Core queries.

This repository provides three packages:

- `EntityFrameworkCore.ConcurrentChunking`
- `EntityFrameworkCore.ConcurrentChunking.Linq`
- `EntityFrameworkCore.ConcurrentChunking.DependencyInjection`

## What It Does

- Splits an ordered query into chunks (`Skip`/`Take` paging).
- Loads chunks concurrently with configurable producer and prefetch limits.
- Streams chunk results through `IAsyncEnumerable`.
- Optionally preserves chunk order at read time.

## Important Constraints

- Your query order must be deterministic and unique (for example `OrderBy(e => e.Id)`).
- Each `ChunkedEntityLoader<TDbContext, TEntity>` instance is single-use.
- `allowUncommittedReads` starts read-uncommitted transactions per producer context.

## Quick Usage

```csharp
await using var context = new MyDbContext();

using var loader = new ChunkedEntityLoader<MyDbContext, MyEntity>(
    dbContextFactory: () => new MyDbContext(),
    chunkSize: 500,
    maxConcurrentProducerCount: 4,
    maxPrefetchCount: 8,
    sourceQueryProvider: db => db.Set<MyEntity>()
        .AsNoTracking()
        .OrderBy(x => x.Id),
    options: ChunkedEntityLoaderOptions.PreserveChunkOrder);

await foreach (var chunk in loader.LoadAsync(CancellationToken.None))
{
    // Process chunk.Entities
}
```

## LINQ Extension Usage

```csharp
await using var context = new MyDbContext();

await foreach (var chunk in context.Set<MyEntity>()
    .AsNoTracking()
    .OrderBy(x => x.Id)
    .LoadChunkedAsync(
        dbContextFactory: () => new MyDbContext(),
        chunkSize: 500,
        maxConcurrentProducerCount: 4,
        maxPrefetchCount: 8,
        options: ChunkedEntityLoaderOptions.PreserveChunkOrder,
        cancellationToken: CancellationToken.None))
{
    // Process chunk.Entities
}
```

## Dependency Injection Usage

```csharp
using Microsoft.EntityFrameworkCore.ConcurrentChunking.DependencyInjection;

var services = new ServiceCollection();

services.AddDbContextFactory<MyDbContext>(options =>
{
    // Configure provider and connection.
});

services.AddChunkedEntityLoaderFactory();

using var serviceProvider = services.BuildServiceProvider();

var loaderFactory = serviceProvider.GetRequiredService<IChunkedEntityLoaderFactory<MyDbContext>>();

using var loader = loaderFactory.Create<MyEntity>(
    chunkSize: 500,
    maxConcurrentProducerCount: 4,
    maxPrefetchCount: 8,
    sourceQueryProvider: db => db.Set<MyEntity>()
        .AsNoTracking()
        .OrderBy(x => x.Id),
    options: ChunkedEntityLoaderOptions.PreserveChunkOrder,
    allowUncommittedReads: false);

await foreach (var chunk in loader.LoadAsync(CancellationToken.None))
{
    // Process chunk.Entities
}
```

