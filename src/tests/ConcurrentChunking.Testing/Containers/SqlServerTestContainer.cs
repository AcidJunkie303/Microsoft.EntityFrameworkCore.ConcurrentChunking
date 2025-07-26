using System.Diagnostics.CodeAnalysis;
using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Entities;
using Testcontainers.MsSql;
using Xunit;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Containers;

[SuppressMessage("ReSharper", "StaticMemberInGenericType")]
[SuppressMessage("Major Code Smell", "S2743:Static fields should not be used in generic types")]
[SuppressMessage("Design", "CA1000:Do not declare static members on generic types")]
[SuppressMessage("Roslynator", "RCS1158:Static member in generic type should use a type parameter")]
public static class SqlServerTestContainer
{
    private static MsSqlContainer? MsSqlContainer;
    private static bool WasAlreadyRunning;
    private static bool AreMigrationsExecuted;
    public static string Password { get; } = "MySecurePa++w0rd";
    public static string ConnectionString { get; } = $"Server=tcp:127.0.0.1,1433;Database=TestDB;User Id=sa;Password={Password};Encrypt=False;TrustServerCertificate=True";

    public static async Task InitializeAsync()
    {
        await EnsureContainerIsRunningAsync();
        await EnsureMigrationsExecutedAsync();
    }

    public static async Task ShutdownAsync()
    {
        if (!WasAlreadyRunning && MsSqlContainer is not null)
        {
            await MsSqlContainer.StopAsync(TestContext.Current.CancellationToken);
            await MsSqlContainer.DisposeAsync();
            MsSqlContainer = null;
        }
    }

    private static async Task EnsureContainerIsRunningAsync()
    {
        if (MsSqlContainer is not null)
        {
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

        await Task.Delay(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);

        MsSqlContainer = container;
    }

    private static async Task EnsureMigrationsExecutedAsync()
    {
        if (AreMigrationsExecuted)
        {
            return;
        }

        await using var dbContext = new SqlServerDbContext();
        if (!await DoesTestDatabaseExistsAsync(dbContext) || !await DoAllTestingTablesExistAsync(dbContext))
        {
            await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        }

        AreMigrationsExecuted = true;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    private static async Task<bool> DoesTestDatabaseExistsAsync(SqlServerDbContext ctx)
    {
        try
        {
            return await ctx.Database.CanConnectAsync(TestContext.Current.CancellationToken);
        }
        catch
        {
            return false;
        }
    }

    private static Task<bool> DoAllTestingTablesExistAsync(SqlServerDbContext ctx)
        => ctx.Database
              .SqlQueryRaw<ItemCount>($"SELECT 1 AS [Count] FROM sys.tables WHERE name = '{nameof(SimpleEntity)}'")
              .AnyAsync();

    private sealed record ItemCount(int Count);
}
