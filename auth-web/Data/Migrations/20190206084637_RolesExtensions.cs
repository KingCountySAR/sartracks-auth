using Microsoft.EntityFrameworkCore.Migrations;

namespace SarData.Auth.Data.Migrations
{
    public partial class RolesExtensions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "auth",
                table: "AspNetRoles",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RoleRoleMembership",
                schema: "auth",
                columns: table => new
                {
                    ParentId = table.Column<string>(nullable: false),
                    ChildId = table.Column<string>(nullable: false),
                    IsDirect = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleRoleMembership", x => new { x.ChildId, x.ParentId });
                    table.ForeignKey(
                        name: "FK_RoleRoleMembership_AspNetRoles_ChildId",
                        column: x => x.ChildId,
                        principalSchema: "auth",
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoleRoleMembership_AspNetRoles_ParentId",
                        column: x => x.ParentId,
                        principalSchema: "auth",
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoleRoleMembership_ParentId",
                schema: "auth",
                table: "RoleRoleMembership",
                column: "ParentId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleRoleMembership",
                schema: "auth");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "auth",
                table: "AspNetRoles");
        }
    }
}
