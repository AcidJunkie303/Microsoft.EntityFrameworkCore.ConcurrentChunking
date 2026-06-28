using ConcurrentChunking.Testing;
using Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Data;
using Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Entities;
using Shouldly;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.Tests;

public sealed class ChunkedEntityLoaderTests : ChunkedEntityLoaderTestBase<InMemoryDbContext, InMemoryTestData>
{
    public ChunkedEntityLoaderTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Theory]
    [InlineData(FailLocationKind.ChunkProductionStart)]
    [InlineData(FailLocationKind.ChunkProductionEnd)]
    public async Task LoadAsync_WhenErrorDuringChunkLoad_ThenExceptionIsReturnedToCaller(FailLocationKind failLocationKind)
    {
        // arrange
        await using var ctx = new InMemoryDbContext();

        var random = $"{Random.Shared.Next()}";
        var sut = ChunkedEntityLoader();

        // act
        var ex = await Record.ExceptionAsync(async () => await sut.LoadAsync(TestContext.Current.CancellationToken).ToListAsync(TestContext.Current.CancellationToken));

        // assert
        ex.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe(random);

        ChunkedEntityLoader<InMemoryDbContext, SimpleEntity> ChunkedEntityLoader()
        {
            return CreateLoader(
                chunkSize: InMemoryTestData.EntityCount,
                maxConcurrentProducerCount: 10,
                maxPrefetchCount: 10,
                options: ChunkedEntityLoaderOptions.None,
                startProducingChunkCallback: StartProducingChunkCallback,
                endProducingChunkCallback: EndProducingChunkCallback);

            Task<object?> StartProducingChunkCallback(IStartCallbackArgs<InMemoryDbContext> args)
                => failLocationKind == FailLocationKind.ChunkProductionStart && args.ChunkIndex == 2
                    ? throw new InvalidOperationException(random)
                    : Task.FromResult<object?>(null);

            Task<object?> EndProducingChunkCallback(IEndCallbackArgs<InMemoryDbContext> args)
                => failLocationKind == FailLocationKind.ChunkProductionEnd && args.ChunkIndex == 2
                    ? throw new InvalidOperationException(random)
                    : Task.FromResult<object?>(null);
        }
    }

    public enum FailLocationKind
    {
        ChunkProductionEnd = 0,
        ChunkProductionStart = 1
    }
}
