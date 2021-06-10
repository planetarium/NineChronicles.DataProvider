using Microsoft.EntityFrameworkCore.Migrations;

namespace NineChronicles.DataProvider.Executable.Migrations
{
    public partial class AddCraftRanking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "StageId",
                table: "HackAndSlashes",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<bool>(
                name: "Mimisbrunnr",
                table: "HackAndSlashes",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)");

            migrationBuilder.AlterColumn<bool>(
                name: "Cleared",
                table: "HackAndSlashes",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)");

            migrationBuilder.AlterColumn<string>(
                name: "AvatarAddress",
                table: "HackAndSlashes",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(767)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AgentAddress",
                table: "HackAndSlashes",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(767)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "HackAndSlashes",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(767)");

            migrationBuilder.AddColumn<long>(
                name: "BlockIndex",
                table: "HackAndSlashes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Avatars",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AgentAddress",
                table: "Avatars",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(767)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Avatars",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(767)");

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Agents",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(767)");

            migrationBuilder.CreateTable(
                name: "CombinationConsumables",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    AgentAddress = table.Column<string>(type: "TEXT", nullable: true),
                    AvatarAddress = table.Column<string>(type: "TEXT", nullable: true),
                    RecipeId = table.Column<int>(type: "INTEGER", nullable: false),
                    SlotIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    BlockIndex = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CombinationConsumables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CombinationConsumables_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CombinationConsumables_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CombinationEquipments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    AgentAddress = table.Column<string>(type: "TEXT", nullable: true),
                    AvatarAddress = table.Column<string>(type: "TEXT", nullable: true),
                    RecipeId = table.Column<int>(type: "INTEGER", nullable: false),
                    SlotIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    SubRecipeId = table.Column<int>(type: "INTEGER", nullable: true),
                    BlockIndex = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CombinationEquipments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CombinationEquipments_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CombinationEquipments_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItemEnhancements",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    AgentAddress = table.Column<string>(type: "TEXT", nullable: true),
                    AvatarAddress = table.Column<string>(type: "TEXT", nullable: true),
                    ItemId = table.Column<string>(type: "TEXT", nullable: true),
                    MaterialId = table.Column<string>(type: "TEXT", nullable: true),
                    SlotIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    BlockIndex = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemEnhancements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemEnhancements_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItemEnhancements_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StageRankings",
                columns: table => new
                {
                    Ranking = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClearedStageId = table.Column<int>(type: "INTEGER", nullable: false),
                    AvatarAddress = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    BlockIndex = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StageRankings", x => x.Ranking);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CombinationConsumables_AgentAddress",
                table: "CombinationConsumables",
                column: "AgentAddress");

            migrationBuilder.CreateIndex(
                name: "IX_CombinationConsumables_AvatarAddress",
                table: "CombinationConsumables",
                column: "AvatarAddress");

            migrationBuilder.CreateIndex(
                name: "IX_CombinationEquipments_AgentAddress",
                table: "CombinationEquipments",
                column: "AgentAddress");

            migrationBuilder.CreateIndex(
                name: "IX_CombinationEquipments_AvatarAddress",
                table: "CombinationEquipments",
                column: "AvatarAddress");

            migrationBuilder.CreateIndex(
                name: "IX_ItemEnhancements_AgentAddress",
                table: "ItemEnhancements",
                column: "AgentAddress");

            migrationBuilder.CreateIndex(
                name: "IX_ItemEnhancements_AvatarAddress",
                table: "ItemEnhancements",
                column: "AvatarAddress");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CombinationConsumables");

            migrationBuilder.DropTable(
                name: "CombinationEquipments");

            migrationBuilder.DropTable(
                name: "ItemEnhancements");

            migrationBuilder.DropTable(
                name: "StageRankings");

            migrationBuilder.DropColumn(
                name: "BlockIndex",
                table: "HackAndSlashes");

            migrationBuilder.AlterColumn<int>(
                name: "StageId",
                table: "HackAndSlashes",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<bool>(
                name: "Mimisbrunnr",
                table: "HackAndSlashes",
                type: "tinyint(1)",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<bool>(
                name: "Cleared",
                table: "HackAndSlashes",
                type: "tinyint(1)",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "AvatarAddress",
                table: "HackAndSlashes",
                type: "varchar(767)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AgentAddress",
                table: "HackAndSlashes",
                type: "varchar(767)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "HackAndSlashes",
                type: "varchar(767)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Avatars",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AgentAddress",
                table: "Avatars",
                type: "varchar(767)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Avatars",
                type: "varchar(767)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Agents",
                type: "varchar(767)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");
        }
    }
}
