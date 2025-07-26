using Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Containers;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.Testing.Entities;

public sealed class SqlServerDbContext : DbContext, IDbContext
{
    private static readonly DbContextOptions<SqlServerDbContext> Options = new DbContextOptionsBuilder<SqlServerDbContext>().Options;
    public DbSet<SimpleEntity> SimpleEntities { get; set; } = null!;

    public SqlServerDbContext() : base(Options)
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
