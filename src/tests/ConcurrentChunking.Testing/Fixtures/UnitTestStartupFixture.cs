using Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Data;
using Xunit;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Fixtures;

public sealed class UnitTestStartupFixture : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await InMemoryTestData.Instance.EnsureInitializedAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
