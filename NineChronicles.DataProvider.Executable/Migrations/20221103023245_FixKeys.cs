using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NineChronicles.DataProvider.Executable.Migrations
{
    public partial class FixKeys : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MigrateMonsterCollections_Agents_AgentAddress1",
                table: "MigrateMonsterCollections");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UnlockWorlds",
                table: "UnlockWorlds");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UnlockEquipmentRecipes",
                table: "UnlockEquipmentRecipes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ShopMaterials",
                table: "ShopMaterials");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MigrateMonsterCollections",
                table: "MigrateMonsterCollections");

            migrationBuilder.DropIndex(
                name: "IX_MigrateMonsterCollections_AgentAddress1",
                table: "MigrateMonsterCollections");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Grindings",
                table: "Grindings");

            migrationBuilder.DropColumn(
                name: "AgentAddress1",
                table: "MigrateMonsterCollections");

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
                name: "ItemId",
                table: "ShopMaterials",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "AgentAddress",
                table: "MigrateMonsterCollections",
                type: "varchar(255)",
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

            migrationBuilder.CreateIndex(
                name: "IX_MigrateMonsterCollections_AgentAddress",
                table: "MigrateMonsterCollections",
                column: "AgentAddress");

            migrationBuilder.AddForeignKey(
                name: "FK_MigrateMonsterCollections_Agents_AgentAddress",
                table: "MigrateMonsterCollections",
                column: "AgentAddress",
                principalTable: "Agents",
                principalColumn: "Address");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MigrateMonsterCollections_Agents_AgentAddress",
                table: "MigrateMonsterCollections");

            migrationBuilder.DropIndex(
                name: "IX_MigrateMonsterCollections_AgentAddress",
                table: "MigrateMonsterCollections");

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
                table: "ShopMaterials",
                keyColumn: "ItemId",
                keyValue: null,
                column: "ItemId",
                value: string.Empty);

            migrationBuilder.AlterColumn<string>(
                name: "ItemId",
                table: "ShopMaterials",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "MigrateMonsterCollections",
                keyColumn: "AgentAddress",
                keyValue: null,
                column: "AgentAddress",
                value: string.Empty);

            migrationBuilder.AlterColumn<string>(
                name: "AgentAddress",
                table: "MigrateMonsterCollections",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AgentAddress1",
                table: "MigrateMonsterCollections",
                type: "varchar(255)",
                nullable: false,
                defaultValue: string.Empty)
                .Annotation("MySql:CharSet", "utf8mb4");

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
                name: "PK_ShopMaterials",
                table: "ShopMaterials",
                column: "ItemId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MigrateMonsterCollections",
                table: "MigrateMonsterCollections",
                column: "AgentAddress");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Grindings",
                table: "Grindings",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_MigrateMonsterCollections_AgentAddress1",
                table: "MigrateMonsterCollections",
                column: "AgentAddress1");

            migrationBuilder.AddForeignKey(
                name: "FK_MigrateMonsterCollections_Agents_AgentAddress1",
                table: "MigrateMonsterCollections",
                column: "AgentAddress1",
                principalTable: "Agents",
                principalColumn: "Address",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
