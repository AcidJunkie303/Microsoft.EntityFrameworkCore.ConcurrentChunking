using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking;

internal static class DbSetAccessor
{
    private static readonly ConcurrentDictionary<(Type, Type), object> CachedDbSetAccessorByType = new();

    public static object Get(DbContext dbContext, Type rootEntityType)
    {
        return CachedDbSetAccessorByType.GetOrAdd(
            (dbContext.GetType(), rootEntityType),
            static types => CreateDbSetAccessor(types.Item2));
    }

    private static object CreateDbSetAccessor(Type entityType)
    {
        var dbSetAccessorType = typeof(DbSetAccessor<,>).MakeGenericType(typeof(DbContext), entityType);
        var funcReturnType = typeof(DbSet<>).MakeGenericType(entityType);
        var funcType = typeof(Func<,>).MakeGenericType(typeof(DbContext), funcReturnType);
        var dbContextParameter = Expression.Parameter(typeof(DbContext), "dbContext");
        var methodInfo = dbSetAccessorType
                            .GetMethod("Get", [typeof(DbContext)])
                         ?? throw new InvalidOperationException("Could not find DbSetAccessor.Get() method.");

        var lambda = Expression.Lambda
        (
            funcType,
            Expression.Call(instance: null, methodInfo, dbContextParameter),
            dbContextParameter
        );

        return lambda.Compile();
    }
}

internal static class DbSetAccessor<TDbContext, TEntity>
    where TDbContext : DbContext
    where TEntity : class
{
    public static DbSet<TEntity> Get(TDbContext dbContext) => dbContext.Set<TEntity>();
}
