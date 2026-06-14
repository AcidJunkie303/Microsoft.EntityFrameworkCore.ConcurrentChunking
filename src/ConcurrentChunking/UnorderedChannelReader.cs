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
        await foreach (var chunk in _channelReader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            _logger?.LogTrace("Obtained chunk with index {ChunkIndex} from underlying queue and passing it to the caller.", chunk.ChunkIndex);
            yield return chunk;
        }

        _logger?.LogTrace("Channel completed for unordered reader.");
    }
}
