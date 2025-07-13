using Microsoft.EntityFrameworkCore.ConcurrentChunking.IntegrationTests.Support;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.IntegrationTests.Entities;

internal sealed class TestDbContext : DbContext
{
    private static readonly DbContextOptions<TestDbContext> Options = new DbContextOptionsBuilder<TestDbContext>().Options;
    public DbSet<SimpleEntity> SimpleEntities { get; set; } = null!;

    public TestDbContext() : base(Options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseSqlServer(SqlServerTestContainer.ConnectionString);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
           .Entity<SimpleEntity>()
           .ToTable(nameof(SimpleEntity), "dbo");

        modelBuilder
           .Entity<SimpleEntity>()
           .Property(e => e.Id)
           .ValueGeneratedNever();

        modelBuilder
           .Entity<SimpleEntity>()
           .HasKey(e => e.Id);
    }
}
