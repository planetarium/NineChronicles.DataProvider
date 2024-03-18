using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NineChronicles.DataProvider.Executable.Migrations
{
    public partial class UpdateCombintaionEquipments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OptionCount",
                table: "CombinationEquipments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "SkillContains",
                table: "CombinationEquipments",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OptionCount",
                table: "CombinationEquipments");

            migrationBuilder.DropColumn(
                name: "SkillContains",
                table: "CombinationEquipments");
        }
    }
}
