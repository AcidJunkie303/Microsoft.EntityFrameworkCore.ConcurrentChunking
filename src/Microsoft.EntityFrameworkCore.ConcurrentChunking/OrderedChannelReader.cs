using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking;

/// <summary>
///     Reads the chunks from the channel in order.
/// </summary>
/// <typeparam name="TEntity">The type of the entity.</typeparam>
internal sealed class OrderedChannelReader<TEntity> : IChannelReader<TEntity>
{
    private readonly ChannelReader<Chunk<TEntity>> _channelReader;
    private readonly Dictionary<int, Chunk<TEntity>> _pendingChunksByIndex = [];

    public OrderedChannelReader(ChannelReader<Chunk<TEntity>> channelReader)
    {
        _channelReader = channelReader;
    }

    public async IAsyncEnumerable<Chunk<TEntity>> ReadAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var expectedIndex = 0;

        while (true)
        {
            // Check if the next expected chunk is already buffered
            if (_pendingChunksByIndex.Remove(expectedIndex, out var bufferedChunk))
            {
                yield return bufferedChunk;
                expectedIndex++;
                continue;
            }

            var chunk = await _channelReader.ReadAsync(cancellationToken).ConfigureAwait(false);
            if (chunk is null) // Null chunk signals completion
            {
                yield break;
            }

            if (chunk.ChunkIndex == expectedIndex)
            {
                yield return chunk;
                expectedIndex++;
            }
            else
            {
                // Buffer out-of-order chunk
                _pendingChunksByIndex[chunk.ChunkIndex] = chunk;
            }
        }
    }
}
