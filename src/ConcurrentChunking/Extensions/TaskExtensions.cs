using System.Diagnostics.CodeAnalysis;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.Extensions;

internal static class TaskExtensions
{
    public static async Task<Task> WhenAnyAsync(
        this IEnumerable<Task> tasks,
        CancellationToken cancellationToken)
    {
        var taskArray = tasks as Task[] ?? tasks.ToArray();

        var cancelTask = Task.Delay(Timeout.Infinite, cancellationToken);
        var winner = await Task.WhenAny(taskArray.Append(cancelTask)).ConfigureAwait(false);

        return winner == cancelTask
            ? throw new OperationCanceledException(cancellationToken)
            : winner;
    }

    [SuppressMessage("Major Code Smell", "S4017:Method signatures should not contain nested generic types")]
    public static async Task<Task<T>> WhenAnyAsync<T>(this IEnumerable<Task<T>> tasks, CancellationToken cancellationToken)
    {
        var taskArray = tasks as Task<T>[] ?? tasks.ToArray();

        var cancelTask = Task.Delay(Timeout.Infinite, cancellationToken);
        var winner = await Task.WhenAny(taskArray.Append(cancelTask)).ConfigureAwait(false);

        if (winner == cancelTask)
        {
            throw new OperationCanceledException(cancellationToken);
        }

        return (Task<T>) winner;
    }
}
