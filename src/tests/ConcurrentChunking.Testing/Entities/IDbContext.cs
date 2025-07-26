namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Entities;

public interface IDbContext
{
    DbSet<SimpleEntity> SimpleEntities { get; set; }
}
