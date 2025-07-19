using Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Entities;
using Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.DependencyInjection.Tests;

public class ServiceProviderExtensionsTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public ServiceProviderExtensionsTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task AddChunkedEntityLoaderFactory_Full()
    {
        await using var services = CreateServiceCollection();
        var chunkedEntityLoaderFactory = services.GetRequiredService<IChunkedEntityLoaderFactory<InMemoryDbContext>>();

        var chunkedLoader = chunkedEntityLoaderFactory.Create(
            1_000,
            5,
            4,
            ctx => ctx.SimpleEntities.OrderBy(a => a.Id),
            ChunkedEntityLoaderOptions.PreserveChunkOrder);

        var chunks = await chunkedLoader.LoadAsync(TestContext.Current.CancellationToken).ToListAsync(TestContext.Current.CancellationToken);

        chunks.ShouldNotBeNull();
        chunks.Count.ShouldBe(11);
    }

    private ServiceProvider CreateServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_testOutputHelper);
        services.AddSingleton<ILoggerFactory, XunitLoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(XunitLogger<>));
        services.AddDbContextFactory<InMemoryDbContext>(options => options.UseInMemoryDatabase("TestDb"));
        services.AddChunkedEntityLoaderFactory();

        return services.BuildServiceProvider();
    }
}
