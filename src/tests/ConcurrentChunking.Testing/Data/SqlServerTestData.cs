using System.Data;
using System.Globalization;
using ConcurrentChunking.Testing.Support;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Entities;
using Xunit;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Data;

public sealed class SqlServerTestData : TestData, ITestData<SqlServerDbContext>
{
    private readonly DbContextFactory<SqlServerDbContext> _dbContextFactory = new(() => new SqlServerDbContext());

    public static ITestData<SqlServerDbContext> Instance { get; } = new SqlServerTestData();
    public static int EntityCount => 1_000_001;

    private SqlServerTestData()
    {
    }

    public IDbContextFactory<SqlServerDbContext> GetDbContextFactory() => _dbContextFactory;

    public SqlServerDbContext CreateDbContext() => _dbContextFactory.CreateDbContext();

    protected override async Task InitializeAsync()
    {
        await using var ctx = new SqlServerDbContext();

        if (await IsCompleteAsync(ctx))
        {
            return;
        }

        await SeedDatabaseAsync(ctx);
    }

    private static async Task SeedDatabaseAsync(SqlServerDbContext ctx)
    {
        await ctx.Database.ExecuteSqlRawAsync("TRUNCATE TABLE dbo.SimpleEntity", TestContext.Current.CancellationToken);

        using var table = CreateSimpleEntitiesTable();

        var connection = (SqlConnection) ctx.Database.GetDbConnection();
        if (connection.State == ConnectionState.Closed)
        {
            await connection.OpenAsync();
        }

        using var sqlBulkCopy = new SqlBulkCopy(connection);
        sqlBulkCopy.DestinationTableName = "dbo.SimpleEntity";
        sqlBulkCopy.BatchSize = 50_000;

        await sqlBulkCopy.WriteToServerAsync(table);
    }

    private static async Task<bool> IsCompleteAsync(SqlServerDbContext ctx)
    {
        var count = await ctx.SimpleEntities.CountAsync(TestContext.Current.CancellationToken);
        var maxId = count > 0
            ? await ctx.SimpleEntities.MaxAsync(a => a.Id, TestContext.Current.CancellationToken)
            : 0;

        return count == EntityCount && maxId == EntityCount;
    }

    private static DataTable CreateSimpleEntitiesTable()
    {
#pragma warning disable CA2000
        var table = new DataTable("SimpleEntities")
        {
            Locale = CultureInfo.InvariantCulture
        };
#pragma warning restore CA2000

        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("Value1", typeof(string));
        table.Columns.Add("Value2", typeof(string));
        table.Columns.Add("Value3", typeof(string));
        table.Columns.Add("Value4", typeof(string));
        table.Columns.Add("Value5", typeof(string));

        for (var i = 1; i <= EntityCount; i++)
        {
            table.Rows.Add(i, $"{i} : 1", $"{i} : 2", $"{i} : 3", $"{i} : 4", $"{i} : 5");
        }

        return table;
    }
}
