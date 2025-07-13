using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore.ConcurrentChunking.IntegrationTests.Entities;
using Testcontainers.MsSql;
using Xunit;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.IntegrationTests.Support;

internal static class SqlServerTestContainer
{
    private static MsSqlContainer? MsSqlContainer;
    private static bool WasAlreadyRunning;
    public static string Password { get; } = "MySecurePa++w0rd";
    public static string ConnectionString { get; } = $"Server=tcp:127.0.0.1,1433;Database=TestDB;User Id=sa;Password={Password};Encrypt=False;TrustServerCertificate=True";

    public static async Task InitializeAsync()
    {
        if (MsSqlContainer is not null)
        {
            await ExecuteMigrationsAsync();
            return;
        }

        var container = new MsSqlBuilder()
                       .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                       .WithReuse(true)
                       .WithPassword($"sa@{Password}")
                       .WithPortBinding(1433, 1433)
                       .WithEnvironment("ACCEPT_EULA", "YES")
                       .WithEnvironment("MSSQL_SA_PASSWORD", Password)
                       .WithName(typeof(SqlServerTestContainer).FullName)
                       .WithHostname("127.0.0.1")
                       .WithWaitStrategy(
                            Wait.ForUnixContainer()
                                .UntilCommandIsCompleted("/opt/mssql-tools18/bin/sqlcmd", "-C", "-Q", "SELECT 303;", "-U", "sa", "-P", Password)
                        )
                       .Build();

        await container.StartAsync(TestContext.Current.CancellationToken);

        WasAlreadyRunning = container.StartedTime.Subtract(DateTimeOffset.UtcNow.DateTime) < TimeSpan.FromSeconds(10);

        await ExecuteMigrationsAsync();

        MsSqlContainer = container;
    }

    public static async Task ShutdownAsync()
    {
        if (!WasAlreadyRunning && MsSqlContainer is not null)
        {
            await MsSqlContainer.StopAsync();
            await MsSqlContainer.DisposeAsync();
            MsSqlContainer = null;
        }
    }

    private static async Task ExecuteMigrationsAsync()
    {
        await using var dbContext = new TestDbContext();
        await dbContext.Database.EnsureDeletedAsync(TestContext.Current.CancellationToken);
        await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
    }
}
