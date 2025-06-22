namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.Tests;

internal sealed class MyDbContext : DbContext
{
    public DbSet<MyEntity> MyEntities { get; set; } = null!;

    public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MyEntity>()
            .Property(e => e.Id)
            .ValueGeneratedNever();
    }
}
