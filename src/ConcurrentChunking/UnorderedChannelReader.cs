using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking;

internal sealed class UnorderedChannelReader<TEntity> : IChannelReader<TEntity>
{
    private readonly ChannelReader<Chunk<TEntity>> _channelReader;
    private readonly ILogger<UnorderedChannelReader<TEntity>>? _logger;

    public UnorderedChannelReader(ChannelReader<Chunk<TEntity>> channelReader, ILogger<UnorderedChannelReader<TEntity>>? logger)
    {
        _channelReader = channelReader;
        _logger = logger;
    }

    public async IAsyncEnumerable<Chunk<TEntity>> ReadAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (true)
        {
            var chunk = await _channelReader.ReadAsync(cancellationToken);
            if (chunk.IsTerminatingChunk) // Null chunk signals completion
            {
                _logger?.LogTrace("Received terminating chunk which indicates completion.");
                yield break;
            }

            _logger?.LogTrace("Obtained chunk with index {ChunkIndex} from underlying queue and passing it to the caller.", chunk.ChunkIndex);
            yield return chunk;
        }
    }
}
