using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NineChronicles.DataProvider.Executable.Migrations
{
    public partial class AddEventDungeon : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventConsumableItemCrafts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AgentAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SlotIndex = table.Column<int>(type: "int", nullable: false),
                    EventScheduleId = table.Column<int>(type: "int", nullable: false),
                    EventConsumableItemRecipeId = table.Column<int>(type: "int", nullable: false),
                    RequiredItem1Id = table.Column<int>(type: "int", nullable: false),
                    RequiredItem1Count = table.Column<int>(type: "int", nullable: false),
                    RequiredItem2Id = table.Column<int>(type: "int", nullable: false),
                    RequiredItem2Count = table.Column<int>(type: "int", nullable: false),
                    RequiredItem3Id = table.Column<int>(type: "int", nullable: false),
                    RequiredItem3Count = table.Column<int>(type: "int", nullable: false),
                    RequiredItem4Id = table.Column<int>(type: "int", nullable: false),
                    RequiredItem4Count = table.Column<int>(type: "int", nullable: false),
                    RequiredItem5Id = table.Column<int>(type: "int", nullable: false),
                    RequiredItem5Count = table.Column<int>(type: "int", nullable: false),
                    RequiredItem6Id = table.Column<int>(type: "int", nullable: false),
                    RequiredItem6Count = table.Column<int>(type: "int", nullable: false),
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventConsumableItemCrafts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventConsumableItemCrafts_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address");
                    table.ForeignKey(
                        name: "FK_EventConsumableItemCrafts_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EventDungeonBattles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AgentAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EventDungeonId = table.Column<int>(type: "int", nullable: false),
                    EventScheduleId = table.Column<int>(type: "int", nullable: false),
                    EventDungeonStageId = table.Column<int>(type: "int", nullable: false),
                    RemainingTickets = table.Column<int>(type: "int", nullable: false),
                    BurntNCG = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Cleared = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FoodsCount = table.Column<int>(type: "int", nullable: false),
                    CostumesCount = table.Column<int>(type: "int", nullable: false),
                    EquipmentsCount = table.Column<int>(type: "int", nullable: false),
                    RewardItem1Id = table.Column<int>(type: "int", nullable: false),
                    RewardItem1Count = table.Column<int>(type: "int", nullable: false),
                    RewardItem2Id = table.Column<int>(type: "int", nullable: false),
                    RewardItem2Count = table.Column<int>(type: "int", nullable: false),
                    RewardItem3Id = table.Column<int>(type: "int", nullable: false),
                    RewardItem3Count = table.Column<int>(type: "int", nullable: false),
                    RewardItem4Id = table.Column<int>(type: "int", nullable: false),
                    RewardItem4Count = table.Column<int>(type: "int", nullable: false),
                    RewardItem5Id = table.Column<int>(type: "int", nullable: false),
                    RewardItem5Count = table.Column<int>(type: "int", nullable: false),
                    RewardItem6Id = table.Column<int>(type: "int", nullable: false),
                    RewardItem6Count = table.Column<int>(type: "int", nullable: false),
                    RewardItem7Id = table.Column<int>(type: "int", nullable: false),
                    RewardItem7Count = table.Column<int>(type: "int", nullable: false),
                    RewardItem8Id = table.Column<int>(type: "int", nullable: false),
                    RewardItem8Count = table.Column<int>(type: "int", nullable: false),
                    RewardItem9Id = table.Column<int>(type: "int", nullable: false),
                    RewardItem9Count = table.Column<int>(type: "int", nullable: false),
                    RewardItem10Id = table.Column<int>(type: "int", nullable: false),
                    RewardItem10Count = table.Column<int>(type: "int", nullable: false),
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventDungeonBattles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventDungeonBattles_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address");
                    table.ForeignKey(
                        name: "FK_EventDungeonBattles_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_EventConsumableItemCrafts_AgentAddress",
                table: "EventConsumableItemCrafts",
                column: "AgentAddress");

            migrationBuilder.CreateIndex(
                name: "IX_EventConsumableItemCrafts_AvatarAddress",
                table: "EventConsumableItemCrafts",
                column: "AvatarAddress");

            migrationBuilder.CreateIndex(
                name: "IX_EventDungeonBattles_AgentAddress",
                table: "EventDungeonBattles",
                column: "AgentAddress");

            migrationBuilder.CreateIndex(
                name: "IX_EventDungeonBattles_AvatarAddress",
                table: "EventDungeonBattles",
                column: "AvatarAddress");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventConsumableItemCrafts");

            migrationBuilder.DropTable(
                name: "EventDungeonBattles");
        }
    }
}
