using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SarData.Auth.Models;

namespace SarData.Auth.Data
{
  public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string, IdentityUserClaim<string>, ApplicationUserRole, IdentityUserLogin<string>, IdentityRoleClaim<string>, IdentityUserToken<string>>
  {
    private readonly string connectionString;

    public DbSet<ExternalLoginCode> ExternalLoginCodes { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public ApplicationDbContext(string connectionString)
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

    protected override void OnModelCreating(ModelBuilder builder)
    {
      base.OnModelCreating(builder);

      if (!string.IsNullOrWhiteSpace(Startup.SqlDefaultSchema))
      {
        builder.HasDefaultSchema(Startup.SqlDefaultSchema);
      }

      builder.Entity<RoleRoleMembership>().HasKey(f => new { f.ChildId, f.ParentId });
      builder.Entity<RoleRoleMembership>().HasOne(f => f.Child).WithMany(f => f.Ancestors);
      builder.Entity<RoleRoleMembership>().HasOne(f => f.Parent).WithMany().HasForeignKey(f => f.ParentId).OnDelete(DeleteBehavior.Restrict);

      builder.Entity<ApplicationUserRole>().HasOne(f => f.Role).WithMany().HasForeignKey(f => new { f.RoleId });
      builder.Entity<ApplicationUserRole>().HasOne(f => f.User).WithMany().HasForeignKey(f => new { f.UserId });

      builder.Entity<ApplicationRole>().HasMany(f => f.UserMembers).WithOne(f => f.Role);
    }
  }
}
