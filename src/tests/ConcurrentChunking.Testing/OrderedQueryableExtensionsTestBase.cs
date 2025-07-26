using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ConcurrentChunking;
using Microsoft.EntityFrameworkCore.ConcurrentChunking.Linq;
using Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Entities;
using Shouldly;
using Xunit;

namespace ConcurrentChunking.Testing;

public abstract partial class OrderedQueryableExtensionsTestBase<TDbContext, TTestData>
{
    [Fact]
    public async Task LoadChunkedAsync_EnsureAllItemsHaveBeenRetrieved()
    {
        // arrange
        await using var ctx = new TDbContext();
        var baseQuery = ctx.SimpleEntities.OrderBy(a => a.Id);

        // act
        var chunks = await baseQuery
                          .LoadChunkedAsync(
                               dbContextFactory: () => new InMemoryDbContext(),
                               chunkSize: 100_000,
                               maxConcurrentProducerCount: 2,
                               maxPrefetchCount: 3,
                               options: ChunkedEntityLoaderOptions.None,
                               loggerFactory: LoggerFactory,
                               cancellationToken: TestContext.Current.CancellationToken)
                          .ToListAsync(TestContext.Current.CancellationToken);

        var items = chunks.SelectMany(a => a.Entities).ToList();
        var uniqueIds = items.Select(a => a.Id).ToHashSet();

        // assert
        items.Count.ShouldBe(EntityCount);

        for (var i = 1; i <= 10_001; i++)
        {
            uniqueIds.Contains(i).ShouldBeTrue($"Id {i} is missing from the retrieved items.");
        }
    }

    [Theory]
    [InlineData(ChunkedEntityLoaderOptions.None, false)]
    [InlineData(ChunkedEntityLoaderOptions.PreserveChunkOrder, true)]
    public async Task LoadChunkedAsync_CheckChunkOrdering(ChunkedEntityLoaderOptions options, bool expectSequentialOrder)
    {
        // arrange
        await using var ctx = new TDbContext();
        var baseQuery = ctx.SimpleEntities.AsNoTracking().OrderBy(a => a.Id);

        // act
        var chunks = await baseQuery
                          .LoadChunkedAsync(
                               () => new TDbContext(),
                               100_000,
                               maxConcurrentProducerCount: 12,
                               maxPrefetchCount: 12,
                               options,
                               LoggerFactory,
                               TestContext.Current.CancellationToken)
                          .ToListAsync(TestContext.Current.CancellationToken);

        // assert
        chunks.Count.ShouldBe(11);

        // The likelihood that the chunks are not sequential is pretty high when the ordering is not enforced (especially when the last chunk is much smaller than the chunk size).
        // Therefore, we assume that the chunks are not sequential.
        IsChunkOrderSequential(chunks).ShouldBe(expectSequentialOrder);
    }
}
