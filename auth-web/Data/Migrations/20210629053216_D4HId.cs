using Microsoft.EntityFrameworkCore.Migrations;

namespace SarData.Auth.Data.Migrations
{
    public partial class D4HId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "D4HId",
                schema: "auth",
                table: "AspNetUsers",
                maxLength: 20,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "D4HId",
                schema: "auth",
                table: "AspNetUsers");
        }
    }
}
