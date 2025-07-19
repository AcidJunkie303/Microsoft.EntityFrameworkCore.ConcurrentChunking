using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking;

internal sealed class OrderedChannelReader<TEntity> : IChannelReader<TEntity>
{
    private readonly ChannelReader<Chunk<TEntity>> _channelReader;
    private readonly ILogger<OrderedChannelReader<TEntity>>? _logger;
    private readonly Dictionary<int, Chunk<TEntity>> _pendingChunksByIndex = [];

    public OrderedChannelReader(ChannelReader<Chunk<TEntity>> channelReader, ILogger<OrderedChannelReader<TEntity>>? logger)
    {
        _channelReader = channelReader;
        _logger = logger;
    }

    public async IAsyncEnumerable<Chunk<TEntity>> ReadAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var expectedIndex = 0;

        while (true)
        {
            // Check if the next expected chunk is already buffered
            if (_pendingChunksByIndex.Remove(expectedIndex, out var bufferedChunk))
            {
                _logger?.LogTrace("Returning out-of-order chunk with index {ChunkIndex}.", expectedIndex);
                yield return bufferedChunk;
                expectedIndex++;

                continue; // start over because we may have more buffered chunks
            }

            var chunk = await _channelReader.ReadAsync(cancellationToken).ConfigureAwait(false);
            if (chunk.IsTerminatingChunk)
            {
                _logger?.LogTrace("Received terminating chunk which indicates completion.");
                yield break;
            }

            if (chunk.ChunkIndex == expectedIndex)
            {
                _logger?.LogTrace("Returning chunk with index {ChunkIndex}.", chunk.ChunkIndex);
                yield return chunk;
                expectedIndex++;
            }
            else
            {
                // Buffer out-of-order chunk
                _logger?.LogTrace("Chunk with index {ChunkIndex} was out of order. Moving to buffer.", chunk.ChunkIndex);
                _pendingChunksByIndex[chunk.ChunkIndex] = chunk;
            }
        }
    }
}
