using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking;

internal sealed class UnorderedChannelReader<TEntity> : IChannelReader<TEntity>
{
    private readonly ChannelReader<object?> _channelReader;
    private readonly ILogger<UnorderedChannelReader<TEntity>>? _logger;

    public UnorderedChannelReader(ChannelReader<object?> channelReader, ILogger<UnorderedChannelReader<TEntity>>? logger)
    {
        _channelReader = channelReader;
        _logger = logger;
    }

    public async IAsyncEnumerable<Chunk<TEntity>> ReadAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (true)
        {
            var item = await _channelReader.ReadAsync(cancellationToken);
            switch (item)
            {
                case null:
                    _logger?.LogTrace("Received null from channel which indicates completion.");
                    yield break;

                case Chunk<TEntity> chunk:
                    _logger?.LogTrace("Obtained chunk with index {ChunkIndex} from underlying queue and passing it to the caller.", chunk.ChunkIndex);
                    yield return chunk;
                    break;

                case Exception ex:
                    throw new InvalidOperationException("Received exception from producer.", ex);

                default:
                    throw new InvalidOperationException($"Unexpected item type: {item.GetType().FullName}");
            }
        }
    }
}
