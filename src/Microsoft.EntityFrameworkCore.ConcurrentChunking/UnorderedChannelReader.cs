using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking;

/// <summary>
///     Reads the chunks from the channel as they become available, without enforcing any re-ordering if necessary..
/// </summary>
/// <typeparam name="TEntity">The type of the entity.</typeparam>
internal sealed class UnorderedChannelReader<TEntity> : IChannelReader<TEntity>
{
    private readonly ChannelReader<Chunk<TEntity>> _channelReader;
    private readonly ILogger<UnorderedChannelReader<TEntity>>? _logger;

    /// <summary>
    ///     Creates a new instance of <see cref="UnorderedChannelReader{TEntity}" />.
    /// </summary>
    /// <param name="channelReader">The underlying channel reader.</param>
    /// <param name="logger">The logger.</param>
    public UnorderedChannelReader(ChannelReader<Chunk<TEntity>> channelReader, ILogger<UnorderedChannelReader<TEntity>>? logger)
    {
        _channelReader = channelReader;
        _logger = logger;
    }

    /// <inheritdoc cref="IChannelReader{TEntity}.ReadAsync" />
    public async IAsyncEnumerable<Chunk<TEntity>> ReadAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (true)
        {
            var chunk = await _channelReader.ReadAsync(cancellationToken);
            if (chunk is null) // Null chunk signals completion
            {
                _logger?.LogTrace("Received null-chunk which indicates completion.");
                yield break;
            }

            _logger?.LogTrace("Obtained chunk with index {ChunkIndex} from underlying queue and passing it to the caller.", chunk.ChunkIndex);
            yield return chunk;
        }
    }
}
