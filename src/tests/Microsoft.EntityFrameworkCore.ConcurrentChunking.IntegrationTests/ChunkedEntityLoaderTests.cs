using Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing;
using Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Entities;
using Shouldly;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.IntegrationTests;

public sealed partial class ChunkedEntityLoaderTests
{
    [Fact]
    public async Task CheckSetup()
    {
        await using var ctx = new SqlServerDbContext();
        var count = await ctx.SimpleEntities.CountAsync(TestContext.Current.CancellationToken);
        count.ShouldBe(IntegrationTestData.EntityCount);
    }

    [Fact]
    public async Task LoadChunkedAsync_EnsureAllItemsHaveBeenRetrieved()
    {
        // arrange
        await using var ctx = new SqlServerDbContext();
        using var sut = CreateLoader(chunkSize: 100_000, maxConcurrentProducerCount: 5, maxPrefetchCount: 2, options: ChunkedEntityLoaderOptions.None);

        // act
        var chunks = await sut.LoadAsync(TestContext.Current.CancellationToken).ToListAsync(TestContext.Current.CancellationToken);
        var items = chunks.SelectMany(a => a.Entities).ToList();
        var uniqueIds = items.Select(a => a.Id).ToHashSet();

        // assert
        items.Count.ShouldBe(IntegrationTestData.EntityCount);

        for (var i = 1; i <= IntegrationTestData.EntityCount; i++)
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
        await using var ctx = new SqlServerDbContext();
        using var sut = CreateLoader(chunkSize: 100_000, maxConcurrentProducerCount: 5, maxPrefetchCount: 3, options: options);

        // act
        var chunks = await sut.LoadAsync(TestContext.Current.CancellationToken).ToListAsync(TestContext.Current.CancellationToken);
        var items = chunks.SelectMany(a => a.Entities).ToList();

        // assert
        items.Count.ShouldBe(IntegrationTestData.EntityCount);

        // The likelihood that the chunks are not sequential is pretty high when the ordering is not enforced (especially when the last chunk is much smaller than the chunk size).
        // Therefore, we assume that the chunks are not sequential.
        IsChunkOrderSequential(chunks).ShouldBe(expectSequentialOrder);
    }
}
