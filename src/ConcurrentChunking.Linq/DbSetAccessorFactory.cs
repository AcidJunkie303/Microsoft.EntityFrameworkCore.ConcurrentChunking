using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.Linq;

internal static class DbSetAccessorFactory
{
    private static readonly ConcurrentDictionary<Type, Func<DbContext, IQueryable>> AccessorCacheByDbContextAndEntityType = new();

    public static Func<DbContext, IQueryable> CreateDbSetAccessor(Type entityType)
    {
        ArgumentNullException.ThrowIfNull(entityType);

        return AccessorCacheByDbContextAndEntityType.GetOrAdd(entityType, CreateDbSetAccessorCore);
    }

    private static Func<DbContext, IQueryable> CreateDbSetAccessorCore(Type entityType)
    {
        var parameter = Expression.Parameter(typeof(DbContext), "dbContext");
        var methodInfo = typeof(DbContext).GetMethod("Set", [])
                         ?? throw new InvalidOperationException($"Could not find {nameof(DbContext)}.{nameof(DbContext.Set)}() method.");

        var genericMethodInfo = methodInfo.MakeGenericMethod(entityType);

        var lambda = Expression.Lambda<Func<DbContext, IQueryable>>
        (
            Expression.Convert(
                Expression.Call(parameter, genericMethodInfo),
                typeof(IQueryable)
            ),
            parameter
        );

        return lambda.Compile();
    }
}
