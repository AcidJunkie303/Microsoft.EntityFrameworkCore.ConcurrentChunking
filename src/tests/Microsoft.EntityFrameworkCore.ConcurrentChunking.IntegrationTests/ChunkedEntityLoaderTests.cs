using Microsoft.EntityFrameworkCore.ConcurrentChunking.IntegrationTests.Entities;
using Shouldly;
using Xunit;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.IntegrationTests;

public sealed partial class ChunkedEntityLoaderTests
{
    [Fact]
    public async Task CheckSetup()
    {
        await using var ctx = new TestDbContext();
        var count = await ctx.SimpleEntities.CountAsync(TestContext.Current.CancellationToken);
        count.ShouldBe(TestData.EntityCount);
    }

    [Fact]
    public async Task LoadAsync_WhenThen()
    {
        // arrange
        await using var ctx = new TestDbContext();
        using var sut = CreateLoader(chunkSize: 100_000, maxConcurrentProducerCount: 5, maxPrefetchCount: 2, options: ChunkedEntityLoaderOptions.None);

        // act
        var chunks = await sut.LoadAsync(TestContext.Current.CancellationToken).ToListAsync(TestContext.Current.CancellationToken);

        // assert
        chunks.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task LoadChunkedAsync_EnsureAllItemsHaveBeenRetrieved()
    {
        // arrange
        await using var ctx = new TestDbContext();
        using var sut = CreateLoader(chunkSize: 100_000, maxConcurrentProducerCount: 5, maxPrefetchCount: 2, options: ChunkedEntityLoaderOptions.None);

        // act
        var chunks = await sut.LoadAsync(TestContext.Current.CancellationToken).ToListAsync(TestContext.Current.CancellationToken);
        var items = chunks.SelectMany(a => a.Entities).ToList();
        var uniqueIds = items.Select(a => a.Id).ToHashSet();

        // assert
        items.Count.ShouldBe(TestData.EntityCount);

        for (var i = 1; i <= TestData.EntityCount; i++)
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
        await using var ctx = new TestDbContext();
        using var sut = CreateLoader(chunkSize: 100_000, maxConcurrentProducerCount: 5, maxPrefetchCount: 2, options: options);

        // act
        var chunks = await sut.LoadAsync(TestContext.Current.CancellationToken).ToListAsync(TestContext.Current.CancellationToken);
        var items = chunks.SelectMany(a => a.Entities).ToList();

        // assert
        items.Count.ShouldBe(TestData.EntityCount);

        // The likelihood that the chunks are not sequential is pretty high when the ordering is not enforced (especially when the last chunk is much smaller than the chunk size).
        // Therefore, we assume that the chunks are not sequential.
        IsChunkOrderSequential(chunks).ShouldBe(expectSequentialOrder);
    }
}
