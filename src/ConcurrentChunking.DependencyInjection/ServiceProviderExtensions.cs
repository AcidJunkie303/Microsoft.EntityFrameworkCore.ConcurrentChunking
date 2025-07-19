using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.DependencyInjection;

/// <summary>
///     Provides extension methods for registering chunked entity loader factories in the dependency injection container.
/// </summary>
public static class ServiceProviderExtensions
{
    /// <summary>
    ///     Registers the <c>IChunkedEntityLoaderFactory&lt;T&gt;</c> and its implementation
    ///     <c>ChunkedEntityLoaderFactory&lt;T&gt;</c> as singletons in the service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add the services to.</param>
    /// <returns>The same <see cref="IServiceCollection" /> instance so that multiple calls can be chained.</returns>
    public static IServiceCollection AddChunkedEntityLoaderFactory(this IServiceCollection services)
        => services.AddSingleton(typeof(IChunkedEntityLoaderFactory<>), typeof(ChunkedEntityLoaderFactory<>));
}
