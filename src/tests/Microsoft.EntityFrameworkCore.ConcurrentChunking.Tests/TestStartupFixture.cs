namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.Tests;

internal sealed class TestStartupFixture : IAsyncLifetime
{
    public ValueTask InitializeAsync()
    {
        TestData.EnsureInitialized();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
