using Shouldly;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.Tests;

public sealed partial class ChunkedEntityLoaderTests
{
    [Fact]
    public async Task CheckSetup()
    {
        await using var ctx = new MyDbContext();
        var count = await ctx.SimpleEntities.CountAsync();
        count.ShouldBe(EntityCount);
    }

    [Fact]
    public async Task LoadAsync_WhenThen()
    {
        // arrange
        await using var ctx = new MyDbContext();
        using var sut = CreateLoader(chunkSize: 1000, maxConcurrentProducerCount: 4, options: ChunkedEntityLoaderOptions.None);

        // act
        var chunks = await sut.LoadAsync(CancellationToken.None).ToListAsync();

        // assert
        chunks.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task LoadChunkedAsync_EnsureAllItemsHaveBeenRetrieved()
    {
        // arrange
        await using var ctx = new MyDbContext();
        using var sut = CreateLoader(chunkSize: 1000, maxConcurrentProducerCount: 4, options: ChunkedEntityLoaderOptions.None);

        // act
        var chunks = await sut.LoadAsync(CancellationToken.None).ToListAsync();
        var items = chunks.SelectMany(a => a.Entities).ToList();
        var uniqueIds = items.Select(a => a.Id).ToHashSet();

        // assert
        items.Count.ShouldBe(EntityCount);

        for (var i = 1; i <= EntityCount; i++)
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
        using var sut = CreateLoader(chunkSize: 1000, maxConcurrentProducerCount: 4, options: options);

        // act
        var chunks = await sut.LoadAsync(CancellationToken.None).ToListAsync();
        var items = chunks.SelectMany(a => a.Entities).ToList();

        // assert
        items.Count.ShouldBe(EntityCount);

        // The likelihood that the chunks are not sequential is pretty high when the ordering is not enforced (especially when the last chunk is much smaller than the chunk size).
        // Therefore, we assume that the chunks are not sequential.
        IsChunkOrderSequential(chunks).ShouldBe(expectSequentialOrder);
    }
}
