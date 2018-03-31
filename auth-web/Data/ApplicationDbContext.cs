using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SarData.Auth.Models;

namespace SarData.Auth.Data
{
  public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
  {
    public DbSet<ExternalLoginCode> ExternalLoginCodes { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
      base.OnModelCreating(builder);

      if (!string.IsNullOrWhiteSpace(Startup.SqlDefaultSchema))
      {
        builder.HasDefaultSchema(Startup.SqlDefaultSchema);
      }
    }
  }
}
