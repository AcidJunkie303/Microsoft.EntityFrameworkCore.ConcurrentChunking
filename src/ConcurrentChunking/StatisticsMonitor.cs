using System.Diagnostics.CodeAnalysis;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking;

[SuppressMessage("Minor Code Smell", "S1227:break statements should not be used except for switch cases")]
internal sealed class StatisticsMonitor
{
    private int _maxActiveProducers;
    private int _activeProducers;

    private int _maxQueueSize;
    private int _queueSize;

    public int MaxActiveProducers => _maxActiveProducers;
    public int MaxQueueSize => _maxQueueSize;

    public int IncrementActiveProducer()
    {
        var newValue = Interlocked.Increment(ref _activeProducers);

        int currentMax;
        do
        {
            currentMax = _maxActiveProducers;
            if (newValue <= currentMax)
            {
                break;
            }
        } while (Interlocked.CompareExchange(ref _maxActiveProducers, newValue, currentMax) != currentMax);

        return newValue;
    }

    public void DecrementActiveProducer() => Interlocked.Decrement(ref _activeProducers);

    public void IncrementQueueSize()
    {
        var newValue = Interlocked.Increment(ref _queueSize);

        int currentMax;
        do
        {
            currentMax = _maxQueueSize;
            if (newValue <= currentMax)
            {
                break;
            }
        } while (Interlocked.CompareExchange(ref _maxQueueSize, newValue, currentMax) != currentMax);
    }

    public void DecrementQueueSize() => Interlocked.Decrement(ref _queueSize);
}
