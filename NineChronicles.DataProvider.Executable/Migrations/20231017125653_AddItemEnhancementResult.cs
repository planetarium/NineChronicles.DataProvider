using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NineChronicles.DataProvider.Executable.Migrations
{
    public partial class AddItemEnhancementResult : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "Exp",
                table: "ItemEnhancements",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Level",
                table: "ItemEnhancements",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SheetId",
                table: "ItemEnhancements",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Exp",
                table: "ItemEnhancements");

            migrationBuilder.DropColumn(
                name: "Level",
                table: "ItemEnhancements");

            migrationBuilder.DropColumn(
                name: "SheetId",
                table: "ItemEnhancements");
        }
    }
}
