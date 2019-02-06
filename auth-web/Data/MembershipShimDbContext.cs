using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SarData.Auth.Data
{
  public class MembershipShimDbContext : DbContext
  {
    private string connectionString;

    public MembershipShimDbContext() { }

    public MembershipShimDbContext(string connectionString)
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

    public DbSet<MemberShimRow> Members { get; set; }
    public DbSet<MemberContactShimRow> Contacts { get; set; }
  
  }
}
