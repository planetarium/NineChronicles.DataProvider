using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NineChronicles.DataProvider.Executable.Migrations
{
    public partial class CustomEquipmentCraft : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomEquipmentCraft",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    AgentAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SlotIndex = table.Column<int>(type: "int", nullable: false),
                    RecipeId = table.Column<int>(type: "int", nullable: false),
                    Relationship = table.Column<int>(type: "int", nullable: false),
                    Scroll = table.Column<int>(type: "int", nullable: false),
                    Circle = table.Column<int>(type: "int", nullable: false),
                    AdditionalMaterials = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EquipmentId = table.Column<int>(type: "int", nullable: false),
                    ElementalType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IconId = table.Column<int>(type: "int", nullable: false),
                    TotalCP = table.Column<long>(type: "bigint", nullable: false),
                    OptionId = table.Column<int>(type: "int", nullable: false),
                    CraftWithRandom = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    HasRandomOnlyIcon = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomEquipmentCraft", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomEquipmentCraft_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address");
                    table.ForeignKey(
                        name: "FK_CustomEquipmentCraft_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CustomEquipmentCraft_AgentAddress",
                table: "CustomEquipmentCraft",
                column: "AgentAddress");

            migrationBuilder.CreateIndex(
                name: "IX_CustomEquipmentCraft_AvatarAddress",
                table: "CustomEquipmentCraft",
                column: "AvatarAddress");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomEquipmentCraft");
        }
    }
}
