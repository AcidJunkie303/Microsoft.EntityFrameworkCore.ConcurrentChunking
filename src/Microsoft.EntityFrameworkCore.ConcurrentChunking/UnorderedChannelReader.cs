using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking;

/// <summary>
///     Reads the chunks from the channel as they become available, without enforcing any order.
/// </summary>
/// <typeparam name="TEntity">The type of the entity.</typeparam>
internal sealed class UnorderedChannelReader<TEntity> : IChannelReader<TEntity>
{
    private readonly ChannelReader<Chunk<TEntity>> _channelReader;

    public UnorderedChannelReader(ChannelReader<Chunk<TEntity>> channelReader)
    {
        _channelReader = channelReader;
    }

    public async IAsyncEnumerable<Chunk<TEntity>> ReadAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (true)
        {
            var chunk = await _channelReader.ReadAsync(cancellationToken);
            if (chunk is null)
            {
                yield break;
            }

            yield return chunk;
        }
    }
}
