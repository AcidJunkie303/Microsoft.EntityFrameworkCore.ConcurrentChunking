using Xunit;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing;

public sealed class UnitTestStartupFixture : IAsyncLifetime
{
    public ValueTask InitializeAsync()
    {
        InMemoryTestData.EnsureInitialized();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
