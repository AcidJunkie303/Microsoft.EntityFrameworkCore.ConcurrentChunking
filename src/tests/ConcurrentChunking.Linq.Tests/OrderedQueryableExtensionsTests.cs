using ConcurrentChunking.Testing;
using Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Data;
using Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Entities;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.Linq.Tests;

public sealed class OrderedQueryableExtensionsTests : OrderedQueryableExtensionsTestBase<InMemoryDbContext, InMemoryTestData>
{
    public OrderedQueryableExtensionsTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }
}
