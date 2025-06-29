using System.Diagnostics;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.IntegrationTests;

public sealed class OrderedQueryableExtensionsTests : IDisposable
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly XunitLoggerFactory _loggerFactory;

    public OrderedQueryableExtensionsTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _loggerFactory = new XunitLoggerFactory(testOutputHelper);
    }

    [Fact]
    public async Task LoadChunkedAsync_EnsureAllItemsHaveBeenRetrieved()
    {
        // arrange
        await using var ctx = new MyDbContext();
        using var loggerFactory = new XunitLoggerFactory(_testOutputHelper);
        var baseQuery = ctx.SimpleEntities
                           .Where(a => a.Id <= 100_001)
                           .OrderBy(a => a.Id);

        // act
        var chunks = await baseQuery
                          .LoadChunkedAsync(() => new MyDbContext(), 10_000, 3, ChunkedEntityLoaderOptions.None, loggerFactory, CancellationToken.None)
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
        using var loggerFactory = new XunitLoggerFactory(_testOutputHelper);
        var baseQuery = ctx.SimpleEntities
                           .Where(a => a.Id <= 100_001)
                           .OrderBy(a => a.Id);

        var stopwatch = Stopwatch.StartNew();

        // act
        var chunks = await baseQuery
                          .LoadChunkedAsync(() => new MyDbContext(), 10_000, 3, options, loggerFactory, CancellationToken.None)
                          .ToListAsync();
        _testOutputHelper.WriteLine($"Test duration {(int) stopwatch.ElapsedMilliseconds} ms.");

        // assert
        chunks.Count.ShouldBe(11);

        // The likelihood that the chunks are not sequential is pretty high when the ordering is not enforced (especially when the last chunk is much smaller than the chunk size).
        // Therefore, we assume that the chunks are not sequential.
        IsChunkOrderSequential(chunks).ShouldBe(expectSequentialOrder);
    }

    public void Dispose() => _loggerFactory.Dispose();

    private static bool IsChunkOrderSequential(in List<Chunk<SimpleEntity>> chunks)
        => !chunks.Where((chunk, i) => chunk.ChunkIndex != i).Any();
}
