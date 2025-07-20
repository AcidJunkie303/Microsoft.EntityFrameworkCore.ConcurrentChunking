namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.DependencyInjection.Tests.Entities;

internal sealed class TestDbContext : DbContext
{
    private static readonly DbContextOptions<TestDbContext> Options = new DbContextOptionsBuilder<TestDbContext>()
                                                                     .UseInMemoryDatabase("TestDb")
                                                                     .Options;

    public DbSet<SimpleEntity> SimpleEntities { get; set; } = null!;

    public TestDbContext() : base(Options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseInMemoryDatabase("TestDb");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SimpleEntity>()
                    .Property(e => e.Id)
                    .ValueGeneratedNever();
    }
}
