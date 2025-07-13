namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.IntegrationTests.Entities;

internal sealed record SimpleEntity(
    int Id,
    string Value1,
    string Value2,
    string Value3,
    string Value4,
    string Value5
);
