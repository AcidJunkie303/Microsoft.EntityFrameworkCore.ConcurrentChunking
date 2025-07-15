using Microsoft.EntityFrameworkCore.ConcurrentChunking.IntegrationTests.Entities;
using Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Logging;
using Shouldly;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.IntegrationTests;

public sealed class OrderedQueryableExtensionsTests : IDisposable
{
    // for the LINQ extensions, we check only if we can retrieve all items from the context in parallel with the copied IQueryable<T> object.

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
        await using var ctx = new TestDbContext();
        using var loggerFactory = new XunitLoggerFactory(_testOutputHelper);
        var baseQuery = ctx.SimpleEntities
                           .Where(a => a.Id <= 100_001)
                           .OrderBy(a => a.Id);

        // act
        var chunks = await baseQuery
                          .LoadChunkedAsync(
                               dbContextFactory: () => new TestDbContext(),
                               chunkSize: 33_333,
                               maxDegreeOfParallelism: 2,
                               maxPrefetchCount: 4,
                               options: ChunkedEntityLoaderOptions.None,
                               loggerFactory: loggerFactory,
                               cancellationToken: TestContext.Current.CancellationToken)
                          .ToListAsync(TestContext.Current.CancellationToken);

        var items = chunks.SelectMany(a => a.Entities).ToList();
        var uniqueIds = items.Select(a => a.Id).ToHashSet();

        // assert
        items.Count.ShouldBe(100_001);

        for (var i = 1; i <= 100_001; i++)
        {
            uniqueIds.Contains(i).ShouldBeTrue($"Id {i} is missing from the retrieved items.");
        }
    }

    public void Dispose() => _loggerFactory.Dispose();
}
