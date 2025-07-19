using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Containers;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing;

[SuppressMessage("ReSharper", "StaticMemberInGenericType")]
[SuppressMessage("Major Code Smell", "S2743:Static fields should not be used in generic types")]
public sealed class IntegrationTestStartupFixture : IAsyncLifetime
{
    private static bool IsRiderHosted { get; } = !Environment.GetEnvironmentVariable("RESHARPER_HOST").IsNullOrEmpty();
    private static bool IsVsHosted { get; } = !Environment.GetEnvironmentVariable("RESHARPER_HOST").IsNullOrEmpty(); // TODO: Find out a way to detect if running in Visual Studio

    public async ValueTask InitializeAsync()
    {
        await SqlServerTestContainer.InitializeAsync();
        await IntegrationTestData.EnsureTestDataAsync();
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
