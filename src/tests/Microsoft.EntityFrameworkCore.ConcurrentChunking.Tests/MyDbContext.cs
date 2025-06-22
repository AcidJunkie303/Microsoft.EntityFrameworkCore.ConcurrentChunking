namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.Tests;

internal sealed class MyDbContext : DbContext
{
    private static readonly DbContextOptions<MyDbContext> Options = new DbContextOptionsBuilder<MyDbContext>()
                                                                   .UseInMemoryDatabase("TestDb")
                                                                   .Options;

    public DbSet<MyEntity> MyEntities { get; set; } = null!;

    public MyDbContext() : base(Options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseInMemoryDatabase("TestDb");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MyEntity>()
                    .Property(e => e.Id)
                    .ValueGeneratedNever();
    }
}
