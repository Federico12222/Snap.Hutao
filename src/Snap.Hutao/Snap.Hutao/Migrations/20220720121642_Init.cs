// <auto-generated />
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Snap.Hutao.Migrations
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "settings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_settings", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    InnerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsSelected = table.Column<bool>(type: "INTEGER", nullable: false),
                    Cookie = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.InnerId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "settings");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
