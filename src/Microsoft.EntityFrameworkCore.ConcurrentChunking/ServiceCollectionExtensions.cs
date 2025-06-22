using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConcurrentChunkingForEntityFramework(this IServiceCollection services) => services;
}
