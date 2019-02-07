using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SarData.Auth.Data.Migrations
{
    public partial class Applications : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Applications",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    Description = table.Column<string>(nullable: false),
                    Url = table.Column<string>(nullable: false),
                    Logo = table.Column<byte[]>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserOrganization",
                schema: "auth",
                columns: table => new
                {
                    UserId = table.Column<string>(nullable: false),
                    OrganizationId = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserOrganization", x => new { x.UserId, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_UserOrganization_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "auth",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationOrganization",
                schema: "auth",
                columns: table => new
                {
                    ApplicationId = table.Column<Guid>(nullable: false),
                    OrganizationId = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationOrganization", x => new { x.ApplicationId, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_ApplicationOrganization_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalSchema: "auth",
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationOrganization",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "UserOrganization",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "Applications",
                schema: "auth");
        }
    }
}
