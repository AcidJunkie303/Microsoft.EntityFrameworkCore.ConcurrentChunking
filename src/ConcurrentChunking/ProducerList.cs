using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.ConcurrentChunking.Extensions;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking;

internal sealed class ProducerList<T>
{
    private readonly Entry?[] _producers;

    public bool IsFull => Array.TrueForAll(_producers, a => a is not null);
    public bool IsEmpty => Array.TrueForAll(_producers, a => a is null);
    public int FreeSlotCount => _producers.Count(a => a is null);

    public ProducerList(int capacity)
    {
        _producers = new Entry[capacity];
    }

    public Task WhenAnyAsync(in CancellationToken cancellationToken) => _producers.Where(a => a is not null).Select(a => a!.Task).WhenAnyAsync(cancellationToken);

    public async Task<Chunk<T>?> TryGetAndRemoveNextCompletedChunkAsync()
    {
        for (var i = 0; i < _producers.Length; i++)
        {
            if (_producers[i] is null)
            {
                continue;
            }

            if (_producers[i]!.Task.IsCompleted)
            {
                var completedTask = _producers[i]!.Task;
                _producers[i] = null;
                return await completedTask;
            }
        }

        return null;
    }

    public async Task<Chunk<T>> GetAndRemoveNextCompletedChunkAsync(CancellationToken cancellationToken)
    {
        await WhenAnyAsync(cancellationToken);

        for (var i = 0; i < _producers.Length; i++)
        {
            if (_producers[i] is null)
            {
                continue;
            }

            if (_producers[i]!.Task.IsCompleted)
            {
                var completedTask = _producers[i]!.Task;
                _producers[i] = null;
                return await completedTask;
            }
        }

        throw new InvalidOperationException("No completed task found in producer list after WhenAnyAsync returned.");
    }

    [SuppressMessage("Major Code Smell", "S4017:Method signatures should not contain nested generic types")]
    public void Add(int chunkIndex, Task<Chunk<T>> task)
    {
        if (IsFull)
        {
            throw new InvalidOperationException("Producer list is full.");
        }

        for (var i = 0; i < _producers.Length; i++)
        {
            if (_producers[i] is null)
            {
                _producers[i] = new Entry(chunkIndex, task);
                return;
            }
        }

        throw new InvalidOperationException("No empty slot in producer list found!");
    }

    public IEnumerable<Task<Chunk<T>>> GetTasks() => _producers.Where(a => a is not null).Select(a => a!.Task);

    private sealed record Entry(int ChunkIndex, Task<Chunk<T>> Task);
}
