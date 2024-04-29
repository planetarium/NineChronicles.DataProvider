using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NineChronicles.DataProvider.Executable.Migrations
{
    public partial class AddRuneLevelBonus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OutputRuneLevelBonus",
                table: "RuneEnhancements",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PreviousRuneLevelBonus",
                table: "RuneEnhancements",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OutputRuneLevelBonus",
                table: "RuneEnhancements");

            migrationBuilder.DropColumn(
                name: "PreviousRuneLevelBonus",
                table: "RuneEnhancements");
        }
    }
}
