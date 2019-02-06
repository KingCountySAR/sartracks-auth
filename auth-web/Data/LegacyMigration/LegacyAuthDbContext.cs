using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SarData.Auth.Data.LegacyMigration
{
  public class LegacyAuthDbContext : DbContext
  {
    private readonly string connectionString;

    public DbSet<AccountRow> Accounts { get; set; }
    public DbSet<RoleRow> Roles { get; set; }
    public DbSet<RoleRoleRow> RoleRoles { get; set; }
    public DbSet<AccountRoleRow> AccountRoles { get; set; }

    public LegacyAuthDbContext(string connectionString)
    {
      this.connectionString = connectionString;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      if (!optionsBuilder.IsConfigured)
      {
        optionsBuilder.UseSqlServer(connectionString);
      }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);
      modelBuilder.HasDefaultSchema("authold");
      modelBuilder.Entity<LoginRow>().HasKey("Provider", "ProviderId");
      modelBuilder.Entity<AccountRoleRow>().HasKey("AccountRow_Id", "RoleRow_Id");
      modelBuilder.Entity<RoleRoleRow>().HasKey("RoleRow_Id", "RoleRow_Id1");
    }
  }
}
