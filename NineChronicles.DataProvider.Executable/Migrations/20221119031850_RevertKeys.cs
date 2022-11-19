using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NineChronicles.DataProvider.Executable.Migrations
{
    public partial class RevertKeys : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "UnlockWorlds",
                keyColumn: "Id",
                keyValue: null,
                column: "Id",
                value: string.Empty);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "UnlockWorlds",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "UnlockEquipmentRecipes",
                keyColumn: "Id",
                keyValue: null,
                column: "Id",
                value: string.Empty);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "UnlockEquipmentRecipes",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Grindings",
                keyColumn: "Id",
                keyValue: null,
                column: "Id",
                value: string.Empty);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Grindings",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UnlockWorlds",
                table: "UnlockWorlds",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UnlockEquipmentRecipes",
                table: "UnlockEquipmentRecipes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Grindings",
                table: "Grindings",
                column: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_UnlockWorlds",
                table: "UnlockWorlds");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UnlockEquipmentRecipes",
                table: "UnlockEquipmentRecipes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Grindings",
                table: "Grindings");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "UnlockWorlds",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "UnlockEquipmentRecipes",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Grindings",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
