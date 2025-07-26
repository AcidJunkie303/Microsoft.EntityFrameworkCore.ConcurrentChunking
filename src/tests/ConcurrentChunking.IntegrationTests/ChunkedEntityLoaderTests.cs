using ConcurrentChunking.Testing;
using Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Data;
using Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Entities;

namespace ConcurrentChunking.IntegrationTests;

public sealed class ChunkedEntityLoaderTests : ChunkedEntityLoaderTestBase<SqlServerDbContext, SqlServerTestData>
{
    public ChunkedEntityLoaderTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }
}
