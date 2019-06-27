using Microsoft.EntityFrameworkCore.Migrations;

namespace SarData.Auth.Data.Migrations
{
  public partial class AddUserFirstLast : Migration
  {
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.AddColumn<string>(
          name: "FirstName",
          schema: "auth",
          table: "AspNetUsers",
          maxLength: 64,
          nullable: true);

      migrationBuilder.AddColumn<string>(
          name: "LastName",
          schema: "auth",
          table: "AspNetUsers",
          maxLength: 128,
          nullable: true);

      migrationBuilder.Sql("UPDATE u SET u.firstname = a.claimvalue, u.lastname = b.claimvalue FROM auth.aspnetusers u left join auth.aspnetuserclaims a on u.id = a.userid and a.claimtype = 'given_name' left join auth.aspnetuserclaims b on u.id = b.userid and b.claimtype = 'family_name'");
      migrationBuilder.Sql("DELETE from auth.aspnetuserclaims where claimtype='family_name' or claimtype='given_name'");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropColumn(
          name: "FirstName",
          schema: "auth",
          table: "AspNetUsers");

      migrationBuilder.DropColumn(
          name: "LastName",
          schema: "auth",
          table: "AspNetUsers");
    }
  }
}
