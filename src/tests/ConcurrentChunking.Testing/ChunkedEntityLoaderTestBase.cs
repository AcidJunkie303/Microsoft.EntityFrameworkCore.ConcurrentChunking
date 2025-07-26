using Microsoft.EntityFrameworkCore.ConcurrentChunking;
using Shouldly;
using Xunit;

namespace ConcurrentChunking.Testing;

public abstract partial class ChunkedEntityLoaderTestBase<TDbContext, TTestData>
{
    [Fact]
    public async Task LoadChunkedAsync_EnsureAllItemsHaveBeenRetrieved()
    {
        // arrange
        await using var ctx = new TDbContext();
        using var sut = CreateLoader(
            chunkSize: TTestData.ChunkSize,
            maxConcurrentProducerCount: 2,
            maxPrefetchCount: 4,
            options: ChunkedEntityLoaderOptions.None);

        // act
        var chunks = await sut.LoadAsync(TestContext.Current.CancellationToken).ToListAsync(TestContext.Current.CancellationToken);
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
        await using var ctx = new TDbContext();
        using var sut = CreateLoader(
            chunkSize: TTestData.ChunkSize,
            maxConcurrentProducerCount: 5,
            maxPrefetchCount: 2,
            options: options,
            chunkProductionStartedCallback: chunkIndex // this will slow down the production of the chunk with index 5 causing it to be out-of-order (most likely)
                => chunkIndex != 5
                    ? Task.CompletedTask
                    : Task.Delay(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken)
        );

        // act
        var chunks = await sut.LoadAsync(TestContext.Current.CancellationToken).ToListAsync(TestContext.Current.CancellationToken);

        // assert
        IsChunkOrderSequential(chunks).ShouldBe(expectSequentialOrder);
    }

    [Theory]
    [InlineData(ChunkedEntityLoaderOptions.None, 1, 10)]
    [InlineData(ChunkedEntityLoaderOptions.None, 4, 7)]
    [InlineData(ChunkedEntityLoaderOptions.None, 8, 3)]
    [InlineData(ChunkedEntityLoaderOptions.None, 10, 1)]
    [InlineData(ChunkedEntityLoaderOptions.PreserveChunkOrder, 1, 10)]
    [InlineData(ChunkedEntityLoaderOptions.PreserveChunkOrder, 4, 7)]
    [InlineData(ChunkedEntityLoaderOptions.PreserveChunkOrder, 8, 3)]
    [InlineData(ChunkedEntityLoaderOptions.PreserveChunkOrder, 10, 1)]
    public async Task LoadChunkedAsync_ArgumentsIsHonored(ChunkedEntityLoaderOptions options, int maxConcurrentProducerCount, int maxPrefetchCount)
    {
        // arrange
        await using var ctx = new TDbContext();
        using var sut = CreateLoader(
            chunkSize: TTestData.EntityCount / 100,
            maxConcurrentProducerCount: maxConcurrentProducerCount,
            maxPrefetchCount: maxPrefetchCount,
            options: options);
        sut.StatisticsMonitor = new StatisticsMonitor();

        // act
        await foreach (var _ in sut.LoadAsync(TestContext.Current.CancellationToken))
        {
            // Simulate some processing time for each chunk
            await Task.Delay(TimeSpan.FromMilliseconds(100), TestContext.Current.CancellationToken);
        }

        // assert
        sut.StatisticsMonitor.MaxActiveProducers.ShouldBe(Math.Min(maxConcurrentProducerCount, maxPrefetchCount)); //max producers cannot be higher than max prefetch count
        sut.StatisticsMonitor.MaxQueueSize.ShouldBe(maxPrefetchCount);

        TestOutputHelper.WriteLine("Arguments:");
        TestOutputHelper.WriteLine($"    options={options} maxConcurrentProducerCount={maxConcurrentProducerCount} maxPrefetchCount={maxPrefetchCount}");
        TestOutputHelper.WriteLine("Statistics:");
        TestOutputHelper.WriteLine($"    ActualMaxActiveProducers={sut.StatisticsMonitor.MaxActiveProducers}");
        TestOutputHelper.WriteLine($"    ActualMaxQueueSize={sut.StatisticsMonitor.MaxQueueSize}");
    }
}
