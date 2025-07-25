﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Data;
using Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Entities;
using Xunit;

namespace ConcurrentChunking.Testing;

public abstract partial class OrderedQueryableExtensionsTestBase<TDbContext, TTestData> : TestBase
    where TDbContext : DbContext, IDbContext, new()
    where TTestData : ITestData<TDbContext>, ITestData
{
    private static int EntityCount => TTestData.EntityCount;

    protected OrderedQueryableExtensionsTestBase(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }
}
