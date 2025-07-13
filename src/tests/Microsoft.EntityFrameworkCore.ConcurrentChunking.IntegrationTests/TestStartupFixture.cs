using Microsoft.EntityFrameworkCore.ConcurrentChunking.IntegrationTests.Support;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.IntegrationTests;

internal sealed class TestStartupFixture : IAsyncLifetime
{
    private static bool IsRiderHosted { get; } = !Environment.GetEnvironmentVariable("RESHARPER_HOST").IsNullOrEmpty();
    private static bool IsVsHosted { get; } = !Environment.GetEnvironmentVariable("RESHARPER_HOST").IsNullOrEmpty(); // TODO: Find out a way to detect if running in Visual Studio

    public async ValueTask InitializeAsync()
    {
        await SqlServerTestContainer.InitializeAsync();
        await TestData.EnsureTestDataAsync();
    }

    public async ValueTask DisposeAsync()
    {
        // we want to keep the test container running if executed from an IDE
        if (IsRiderHosted || IsVsHosted)
        {
            return;
        }

        await SqlServerTestContainer.ShutdownAsync();
    }
}
