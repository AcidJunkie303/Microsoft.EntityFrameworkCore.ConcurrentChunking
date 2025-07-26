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
    public async Task AddChunkedEntityLoaderFactory_ThenFactoryCanCreateLoader()
    {
        await using var services = CreateServiceCollection();
        var chunkedEntityLoaderFactory = services.GetRequiredService<IChunkedEntityLoaderFactory<InMemoryDbContext>>();

        var chunkedLoader = chunkedEntityLoaderFactory.Create(
            100_000,
            5,
            4,
            ctx => ctx.SimpleEntities.AsNoTracking().OrderBy(a => a.Id));

        chunkedLoader.ShouldNotBeNull();
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
