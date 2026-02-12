using FactorySnap.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace FactorySnap.Shared.Data;

public class FactoryContext(DbContextOptions<FactoryContext> options) : DbContext(options)
{
  public DbSet<Measurement> Measurements { get; set; }
  public DbSet<OpcServerConfig> OpcServers { get; set; }
  public DbSet<OpcNodeConfig> OpcNodes { get; set; }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(FactoryContext).Assembly);
  }

  public async Task EnsureTimescaleEnabled()
  {
    await Database.EnsureCreatedAsync();

    try
    {
      await Database.ExecuteSqlRawAsync(
        "SELECT create_hypertable('\"Measurements\"', 'Timestamp', if_not_exists => TRUE);");
    }
    catch (Exception ex)
    {
      Console.WriteLine(ex);
    }
  }
}