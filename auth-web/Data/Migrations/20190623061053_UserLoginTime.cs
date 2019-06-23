using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SarData.Auth.Data.Migrations
{
    public partial class UserLoginTime : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastLogin",
                schema: "auth",
                table: "AspNetUsers",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastLogin",
                schema: "auth",
                table: "AspNetUsers");
        }
    }
}
