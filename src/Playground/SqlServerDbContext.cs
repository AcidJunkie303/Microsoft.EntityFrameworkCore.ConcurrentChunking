using Microsoft.EntityFrameworkCore;

namespace Playground;

public sealed class SqlServerDbContext : DbContext
{
    private static readonly DbContextOptions<SqlServerDbContext> Options = new DbContextOptionsBuilder<SqlServerDbContext>().Options;
    public static string Password { get; } = "MySecurePa++w0rd";

    public DbSet<SimpleEntity> SimpleEntities { get; set; } = null!;

    public SqlServerDbContext() : base(Options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseSqlServer($"Server=tcp:127.0.0.1,1433;Database=TestDB;User Id=sa;Password={Password};Encrypt=False;TrustServerCertificate=True");

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
