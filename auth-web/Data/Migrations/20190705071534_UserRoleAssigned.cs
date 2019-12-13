using Microsoft.EntityFrameworkCore.Migrations;

namespace SarData.Auth.Data.Migrations
{
  public partial class UserRoleAssigned : Migration
  {
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.AddColumn<bool>(
          name: "Assigned",
          schema: "auth",
          table: "AspNetUserRoles",
          nullable: false,
          defaultValue: false);

      migrationBuilder.Sql("update auth.AspNetUserRoles set assigned = 1");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.Sql("DELETE FROM auth.AspNetUserRoles WHERE assigned = 0");
      migrationBuilder.DropColumn(
          name: "Assigned",
          schema: "auth",
          table: "AspNetUserRoles");
    }
  }
}
