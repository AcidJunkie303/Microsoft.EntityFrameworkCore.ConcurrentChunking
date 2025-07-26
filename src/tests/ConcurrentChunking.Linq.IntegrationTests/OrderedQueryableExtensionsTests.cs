using ConcurrentChunking.Testing;
using Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Data;
using Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Entities;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.Linq.IntegrationTests;

public sealed class OrderedQueryableExtensionsTests : OrderedQueryableExtensionsTestBase<SqlServerDbContext, SqlServerTestData>
{
    public OrderedQueryableExtensionsTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }
}
