using ConcurrentChunking.Testing;
using Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Data;
using Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Entities;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.Tests;

public sealed class ChunkedEntityLoaderTests : ChunkedEntityLoaderTestBase<InMemoryDbContext, InMemoryTestData>
{
    public ChunkedEntityLoaderTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }
}
