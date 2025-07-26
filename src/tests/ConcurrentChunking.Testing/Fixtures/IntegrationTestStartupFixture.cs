using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Containers;
using Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Data;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Fixtures;

[SuppressMessage("ReSharper", "StaticMemberInGenericType")]
[SuppressMessage("Major Code Smell", "S2743:Static fields should not be used in generic types")]
public sealed class IntegrationTestStartupFixture : IAsyncLifetime
{
    private static bool IsRiderHosted { get; } = !Environment.GetEnvironmentVariable("RESHARPER_HOST").IsNullOrEmpty();
    private static bool IsVsHosted { get; } = !Environment.GetEnvironmentVariable("VisualStudioEdition").IsNullOrEmpty();

    public async ValueTask InitializeAsync()
    {
        await SqlServerTestContainer.InitializeAsync();
        await SqlServerTestData.Instance.EnsureInitializedAsync();
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
