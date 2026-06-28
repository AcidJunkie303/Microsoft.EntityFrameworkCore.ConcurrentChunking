# EntityFrameworkCore.ConcurrentChunking

`EntityFrameworkCore.ConcurrentChunking` helps load large EF Core queries in **parallel chunks** with optional prefetching.

It is useful when a single large query takes too long to materialize before processing can begin, especially for workloads like:

- data migration
- reporting/export pipelines
- batch processing and background jobs

## Packages

- `EntityFrameworkCore.ConcurrentChunking` - core loader API
- `EntityFrameworkCore.ConcurrentChunking.Linq` - `IQueryable<T>` extension methods
- `EntityFrameworkCore.ConcurrentChunking.DependencyInjection` - DI factory registration

## Target frameworks

This repository currently targets:

- `net8.0`
- `net9.0`
- `net10.0`

## Installation

Install the package(s) you need:

```powershell
dotnet add package EntityFrameworkCore.ConcurrentChunking
dotnet add package EntityFrameworkCore.ConcurrentChunking.Linq
dotnet add package EntityFrameworkCore.ConcurrentChunking.DependencyInjection
```

## Quick start

### 1) Core loader (`ChunkedEntityLoader<TDbContext, TEntity>`)

```csharp
using Microsoft.EntityFrameworkCore.ConcurrentChunking;

var loader = new ChunkedEntityLoader<TestDbContext, SimpleEntity>(
    dbContextFactory: () => new TestDbContext(),
    chunkSize: 100_000,
    maxConcurrentProducerCount: 3,
    maxPrefetchCount: 5,
    sourceQueryProvider: ctx => ctx.SimpleEntities
        .Include(e => e.RelatedEntity)
            .ThenInclude(e => e.AnotherRelatedEntity)
        .Include(e => e.RelatedEntity2)
        .AsNoTracking()
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

### 2) LINQ extension (`LoadChunkedAsync`)

`LoadChunkedAsync` requires an ordered query (`OrderBy` or `OrderByDescending`) to ensure stable chunking.

```csharp
using Microsoft.EntityFrameworkCore.ConcurrentChunking;
using Microsoft.EntityFrameworkCore.ConcurrentChunking.Linq;

await using var ctx = new TestDbContext();

var chunks = ctx.TestEntities
    .Include(e => e.RelatedEntity)
        .ThenInclude(e => e.AnotherRelatedEntity)
    .Include(e => e.RelatedEntity2)
    .AsNoTracking()
    .OrderByDescending(e => e.Id)
    .LoadChunkedAsync(
        dbContextFactory: () => new TestDbContext(),
        chunkSize: 100_000,
        maxConcurrentProducerCount: 3,
        maxPrefetchCount: 5,
        options: ChunkedEntityLoaderOptions.PreserveChunkOrder);

await foreach (var chunk in chunks)
{
    foreach (var entity in chunk.Entities)
    {
        Console.WriteLine($"Entity ID: {entity.Id}");
    }
}
```

### 3) Dependency injection (`IChunkedEntityLoaderFactory<TDbContext>`)

Register the factory:

```csharp
using Microsoft.EntityFrameworkCore.ConcurrentChunking.DependencyInjection;

services.AddChunkedEntityLoaderFactory();
```

Create and consume a loader from DI:

```csharp
var factory = services.GetRequiredService<IChunkedEntityLoaderFactory<TestDbContext>>();

var loader = factory.Create(
    chunkSize: 100_000,
    maxConcurrentProducerCount: 3,
    maxPrefetchCount: 5,
    sourceQueryProvider: ctx => ctx.SimpleEntities
        .Include(e => e.RelatedEntity)
            .ThenInclude(e => e.AnotherRelatedEntity)
        .Include(e => e.RelatedEntity2)
        .AsNoTracking()
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

## Advanced Usage

### Chunk production callbacks

All loader entry points support two optional callbacks:

- `startProducingChunkCallback` runs before a chunk query starts.
- `endProducingChunkCallback` runs after the chunk query finishes.

Both callbacks receive `ICallbackArgs<TContext>`, which exposes:

- `ChunkIndex` - the zero-based chunk number (read-only).
- `DbContext` - the context used for the chunk. You can replace it in the start callback if you need a custom instance.
- `State` - any value you want to pass from the start callback to the end callback.

This is useful for chunk-level logging, correlation IDs, metrics, or swapping in a specialized context for a specific chunk.

```csharp
using Microsoft.EntityFrameworkCore.ConcurrentChunking;

var loader = new ChunkedEntityLoader<TestDbContext, SimpleEntity>(
    dbContextFactory: () => new TestDbContext(),
    chunkSize: 100_000,
    maxConcurrentProducerCount: 3,
    maxPrefetchCount: 5,
    sourceQueryProvider: ctx => ctx.SimpleEntities.OrderBy(e => e.Id),
    startProducingChunkCallback: args =>
    {
        args.State = Stopwatch.GetTimestamp();

        if (args.ChunkIndex == 0)
        {
            args.DbContext = new TestDbContext();
        }

        return Task.CompletedTask;
    },
    endProducingChunkCallback: args =>
    {
        if (args.State is long startedAt)
        {
            var elapsed = Stopwatch.GetElapsedTime(startedAt);
            Console.WriteLine($"Chunk {args.ChunkIndex} completed in {elapsed.TotalMilliseconds:n0} ms.");
        }

        return Task.CompletedTask;
    });
```

The same callbacks are available on `LoadChunkedAsync(...)` and on `IChunkedEntityLoaderFactory<TDbContext>.Create(...)`, so you can use the same pattern whether you construct the loader directly, through LINQ, or from dependency injection.

## Tuning guidance

- `chunkSize`: larger chunks reduce round-trips but increase memory per chunk.
- `maxConcurrentProducerCount`: controls how many chunk producers run in parallel.
- `maxPrefetchCount`: limits queued chunk count and therefore memory pressure.
- `PreserveChunkOrder`: keeps output ordered by chunk index; disable if out-of-order consumption is acceptable.

Start conservatively, measure DB pressure and memory usage, then increase settings incrementally.

## Notes and caveats

- Always use deterministic ordering in source queries.
- Consider `AsNoTracking()` for read-only workloads.
- Each producer uses its own `DbContext` instance; make sure your factory is safe for concurrent use.

## Build and test

From `src`:

```powershell
dotnet restore
dotnet build -c Release
dotnet test -c Release
```

## License

MIT. See `LICENSE.txt`.
