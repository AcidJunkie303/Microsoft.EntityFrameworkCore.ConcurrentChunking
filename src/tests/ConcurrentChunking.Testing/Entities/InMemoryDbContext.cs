namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Entities;

public sealed class InMemoryDbContext : DbContext
{
    private static readonly DbContextOptions<InMemoryDbContext> Options = new DbContextOptionsBuilder<InMemoryDbContext>()
                                                                         .UseInMemoryDatabase("TestDb")
                                                                         .Options;

    public DbSet<SimpleEntity> SimpleEntities { get; set; } = null!;

    public InMemoryDbContext() : base(Options)
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
