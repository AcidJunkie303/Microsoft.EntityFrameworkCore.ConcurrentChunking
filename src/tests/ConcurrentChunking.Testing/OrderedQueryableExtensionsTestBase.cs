using Microsoft.EntityFrameworkCore.ConcurrentChunking;
using Microsoft.EntityFrameworkCore.ConcurrentChunking.Linq;
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
                               dbContextFactory: () => new TDbContext(),
                               chunkSize: TTestData.ChunkSize,
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
}
