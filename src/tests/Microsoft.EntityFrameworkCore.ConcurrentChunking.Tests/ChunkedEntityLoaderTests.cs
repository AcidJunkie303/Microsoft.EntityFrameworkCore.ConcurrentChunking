using Shouldly;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.Tests;

public sealed partial class ChunkedEntityLoaderTests
{
    [Fact]
    public async Task CheckSetup()
    {
        await using var ctx = new MyDbContext();
        var count = await ctx.MyEntities.CountAsync();
        count.ShouldBe(EntityCount);
    }

    [Fact]
    public async Task LoadAsync_()
    {
        // arrange
        await using var ctx = new MyDbContext();
        using var sut = CreateLoader(chunkSize: 1000, maxConcurrentProducerCount: 4, options: ChunkedEntityLoaderOptions.None);

        // act
        var chunks = await sut.LoadAsync(CancellationToken.None).ToListAsync();

        // assert
    }
}
