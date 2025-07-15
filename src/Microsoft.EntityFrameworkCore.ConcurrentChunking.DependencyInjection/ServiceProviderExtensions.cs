using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.DependencyInjection;

public static class ServiceProviderExtensions
{
    public static IServiceCollection AddChunkedEntityLoaderFactory(this IServiceCollection services)
        => services.AddSingleton(typeof(IChunkedEntityLoaderFactory<>), typeof(ChunkedEntityLoaderFactory<>));
}
