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
    [InlineData(FailLocationKind.ChunkProductionInnerStart)]
    [InlineData(FailLocationKind.ChunkProductionEnd)]
    [InlineData(FailLocationKind.ChunkProductionInnerEnd)]
    public async Task LoadAsync_WhenErrorDuringChunkLoad_ThenExceptionIsReturnedToCaller(FailLocationKind failLocationKind)
    {
        // arrange
        await using var ctx = new InMemoryDbContext();

        var random = $"{Random.Shared.Next()}";
        using var sut = ChunkedEntityLoader();

        // act
        var ex = await Record.ExceptionAsync(async () => await sut.LoadAsync(TestContext.Current.CancellationToken).ToListAsync(TestContext.Current.CancellationToken));

        // assert
        ex.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe(random);

        ChunkedEntityLoader<InMemoryDbContext, SimpleEntity> ChunkedEntityLoader()
        {
            ChunkedEntityLoader<InMemoryDbContext, SimpleEntity>? chunkedEntityLoader = null;
            try
            {
                chunkedEntityLoader = CreateLoader(
                    chunkSize: InMemoryTestData.EntityCount,
                    maxConcurrentProducerCount: 10,
                    maxPrefetchCount: 10,
                    options: ChunkedEntityLoaderOptions.None,
                    chunkProductionStartedCallback: null);

                switch (failLocationKind)
                {
                    case FailLocationKind.ChunkProductionEnd:
                        chunkedEntityLoader.ChunkProductionEnd = _ => throw new InvalidOperationException(random);
                        break;
                    case FailLocationKind.ChunkProductionInnerEnd:
                        chunkedEntityLoader.ChunkProductionInnerEnd = _ => throw new InvalidOperationException(random);
                        break;
                    case FailLocationKind.ChunkProductionInnerStart:
                        chunkedEntityLoader.ChunkProductionInnerStart = _ => throw new InvalidOperationException(random);
                        break;
                    case FailLocationKind.ChunkProductionStart:
                        chunkedEntityLoader.ChunkProductionStart = _ => throw new InvalidOperationException(random);
                        break;

                    default:
#pragma warning disable S3928
                        throw new ArgumentOutOfRangeException(nameof(failLocationKind), failLocationKind, message: null);
#pragma warning restore S3928
                }

                return chunkedEntityLoader;
            }
            catch
            {
                chunkedEntityLoader?.Dispose();
                throw;
            }
        }
    }

    public enum FailLocationKind
    {
        ChunkProductionEnd = 0,
        ChunkProductionInnerEnd = 1,
        ChunkProductionInnerStart = 2,
        ChunkProductionStart = 3
    }
}
