using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NineChronicles.DataProvider.Executable.Migrations
{
    public partial class AddUnlockRuneSlotCrystal : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BurntCRYSTAL",
                table: "UnlockRuneSlots",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BurntCRYSTAL",
                table: "UnlockRuneSlots");
        }
    }
}
