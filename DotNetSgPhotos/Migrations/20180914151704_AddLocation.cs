using System.Data.SqlTypes;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DotNetSgPhotos.Migrations
{
    public partial class AddLocation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<SqlBytes>(
                name: "Location",
                table: "Photos",
                type: "Geometry",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Location",
                table: "Photos");
        }
    }
}
