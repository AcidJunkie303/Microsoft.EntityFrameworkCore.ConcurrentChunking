using Shouldly;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.Tests;

public sealed partial class OrderedQueryableExtensionsTests : IDisposable
{
    [Fact]
    public async Task LoadChunkedAsync_EnsureAllItemsHaveBeenRetrieved()
    {
        // arrange
        await using var ctx = new MyDbContext();
        var baseQuery = ctx.SimpleEntities
                           .Where(a => a.Id <= 100_001)
                           .OrderBy(a => a.Id);

        // act
        var chunks = await baseQuery
                          .LoadChunkedAsync(() => new MyDbContext(), 10_000, 3, ChunkedEntityLoaderOptions.None, _loggerFactory, CancellationToken.None)
                          .ToListAsync();

        var items = chunks.SelectMany(a => a.Entities).ToList();
        var uniqueIds = items.Select(a => a.Id).ToHashSet();

        // assert
        items.Count.ShouldBe(100_001);

        for (var i = 1; i <= 100_001; i++)
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
        await using var ctx = new MyDbContext();
        var baseQuery = ctx.SimpleEntities
                           .Where(a => a.Id <= 100_001)
                           .OrderBy(a => a.Id);

        // act
        var chunks = await baseQuery
                          .LoadChunkedAsync(() => new MyDbContext(), 10_000, 3, options, _loggerFactory, CancellationToken.None)
                          .ToListAsync();

        // assert
        chunks.Count.ShouldBe(11);

        // The likelihood that the chunks are not sequential is pretty high when the ordering is not enforced (especially when the last chunk is much smaller than the chunk size).
        // Therefore, we assume that the chunks are not sequential.
        IsChunkOrderSequential(chunks).ShouldBe(expectSequentialOrder);
    }
}
