using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NineChronicles.DataProvider.Executable.Migrations
{
    public partial class UpdateTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Stakings",
                table: "Stakings");

            migrationBuilder.AddColumn<decimal>(
                name: "BurntNCG",
                table: "ItemEnhancements",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BurntNCG",
                table: "ItemEnhancements");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Stakings",
                table: "Stakings",
                column: "TimeStamp");
        }
    }
}
