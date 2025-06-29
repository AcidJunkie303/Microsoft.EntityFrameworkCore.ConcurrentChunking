namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.IntegrationTests;

internal sealed class MyDbContext : DbContext
{
    private const string ConnectionString = "Server=127.0.0.1,1433;Database=Db1;User Id=sa;Password=Unknown123+;Encrypt=False;TrustServerCertificate=True";
    private static readonly DbContextOptions<MyDbContext> Options = new DbContextOptionsBuilder<MyDbContext>().Options;
    public DbSet<SimpleEntity> SimpleEntities { get; set; } = null!;

    public MyDbContext() : base(Options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseSqlServer(ConnectionString);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
           .Entity<SimpleEntity>()
           .ToTable("T_SimpleEntity", "dbo");

        modelBuilder
           .Entity<SimpleEntity>()
           .Property(e => e.Id)
           .ValueGeneratedNever();

        modelBuilder
           .Entity<SimpleEntity>()
           .HasKey(e => e.Id);
    }
}
