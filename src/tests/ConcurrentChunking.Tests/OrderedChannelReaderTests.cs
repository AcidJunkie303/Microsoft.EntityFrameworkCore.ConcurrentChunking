using System.Threading.Channels;
using Shouldly;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.Tests;

public sealed class OrderedChannelReaderTests
{
    [Fact]
    public async Task ReadAsync_WhenChunksArriveOutOfOrder_ReturnsChunksInOrder()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var channel = Channel.CreateUnbounded<Chunk<int>>();
        var reader = new OrderedChannelReader<int>(channel.Reader, logger: null);

        await channel.Writer.WriteAsync(new Chunk<int>(2, [3]), cancellationToken);
        await channel.Writer.WriteAsync(new Chunk<int>(0, [1]), cancellationToken);
        await channel.Writer.WriteAsync(new Chunk<int>(1, [2]), cancellationToken);
        channel.Writer.TryComplete();

        var chunks = await reader.ReadAsync(cancellationToken).ToListAsync(cancellationToken);

        chunks.Select(c => c.ChunkIndex).ShouldBe([0, 1, 2]);
        chunks.SelectMany(c => c.Entities).ShouldBe([1, 2, 3]);
    }

    [Fact]
    public async Task ReadAsync_WhenChannelCompletesWithMissingChunk_ThrowsInvalidOperationException()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var channel = Channel.CreateUnbounded<Chunk<int>>();
        var reader = new OrderedChannelReader<int>(channel.Reader, logger: null);

        await channel.Writer.WriteAsync(new Chunk<int>(1, [2]), cancellationToken);
        channel.Writer.TryComplete();

        var exception = await Should.ThrowAsync<InvalidOperationException>(async () => await reader.ReadAsync(cancellationToken).ToListAsync(cancellationToken));

        exception.Message.ShouldContain("Missing chunk index 0");
    }
}
