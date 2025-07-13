using Microsoft.EntityFrameworkCore;

namespace Playground;

internal sealed class TestDbContext : DbContext
{
    private static readonly DbContextOptions<TestDbContext> Options = new DbContextOptionsBuilder<TestDbContext>()
                                                                     .UseInMemoryDatabase("TestDb")
                                                                     .Options;

    public DbSet<TestEntity> TestEntities { get; set; } = null!;

    public TestDbContext() : base(Options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseInMemoryDatabase("TestDb");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestEntity>()
                    .Property(e => e.Id)
                    .ValueGeneratedNever();
    }
}
