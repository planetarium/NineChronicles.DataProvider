using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NineChronicles.DataProvider.Executable.Migrations
{
    public partial class AddBattleGrandFinale : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BattleGrandFinales",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    AgentAddress = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarAddress = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarLevel = table.Column<int>(type: "int", nullable: false),
                    EnemyAvatarAddress = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GrandFinaleId = table.Column<int>(type: "int", nullable: false),
                    Victory = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    GrandFinaleScore = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BattleGrandFinales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BattleGrandFinales_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BattleGrandFinales_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EventMaterialItemCrafts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AgentAddress = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarAddress = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EventScheduleId = table.Column<int>(type: "int", nullable: false),
                    EventMaterialItemRecipeId = table.Column<int>(type: "int", nullable: false),
                    Material1Id = table.Column<int>(type: "int", nullable: false),
                    Material1Count = table.Column<int>(type: "int", nullable: false),
                    Material2Id = table.Column<int>(type: "int", nullable: false),
                    Material2Count = table.Column<int>(type: "int", nullable: false),
                    Material3Id = table.Column<int>(type: "int", nullable: false),
                    Material3Count = table.Column<int>(type: "int", nullable: false),
                    Material4Id = table.Column<int>(type: "int", nullable: false),
                    Material4Count = table.Column<int>(type: "int", nullable: false),
                    Material5Id = table.Column<int>(type: "int", nullable: false),
                    Material5Count = table.Column<int>(type: "int", nullable: false),
                    Material6Id = table.Column<int>(type: "int", nullable: false),
                    Material6Count = table.Column<int>(type: "int", nullable: false),
                    Material7Id = table.Column<int>(type: "int", nullable: false),
                    Material7Count = table.Column<int>(type: "int", nullable: false),
                    Material8Id = table.Column<int>(type: "int", nullable: false),
                    Material8Count = table.Column<int>(type: "int", nullable: false),
                    Material9Id = table.Column<int>(type: "int", nullable: false),
                    Material9Count = table.Column<int>(type: "int", nullable: false),
                    Material10Id = table.Column<int>(type: "int", nullable: false),
                    Material10Count = table.Column<int>(type: "int", nullable: false),
                    Material11Id = table.Column<int>(type: "int", nullable: false),
                    Material11Count = table.Column<int>(type: "int", nullable: false),
                    Material12Id = table.Column<int>(type: "int", nullable: false),
                    Material12Count = table.Column<int>(type: "int", nullable: false),
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    Date = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventMaterialItemCrafts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventMaterialItemCrafts_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventMaterialItemCrafts_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_BattleGrandFinales_AgentAddress",
                table: "BattleGrandFinales",
                column: "AgentAddress");

            migrationBuilder.CreateIndex(
                name: "IX_BattleGrandFinales_AvatarAddress",
                table: "BattleGrandFinales",
                column: "AvatarAddress");

            migrationBuilder.CreateIndex(
                name: "IX_EventMaterialItemCrafts_AgentAddress",
                table: "EventMaterialItemCrafts",
                column: "AgentAddress");

            migrationBuilder.CreateIndex(
                name: "IX_EventMaterialItemCrafts_AvatarAddress",
                table: "EventMaterialItemCrafts",
                column: "AvatarAddress");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BattleGrandFinales");

            migrationBuilder.DropTable(
                name: "EventMaterialItemCrafts");
        }
    }
}
