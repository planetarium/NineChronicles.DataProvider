using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NineChronicles.DataProvider.Executable.Migrations
{
    public partial class AddAgentAddress : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AgentAddress",
                table: "AdventureBossWanted",
                type: "varchar(255)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AgentAddress",
                table: "AdventureBossUnlockFloor",
                type: "varchar(255)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AgentAddress",
                table: "AdventureBossRush",
                type: "varchar(255)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AgentAddress",
                table: "AdventureBossClaimReward",
                type: "varchar(255)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AgentAddress",
                table: "AdventureBossChallenge",
                type: "varchar(255)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AdventureBossWanted_AgentAddress",
                table: "AdventureBossWanted",
                column: "AgentAddress");

            migrationBuilder.CreateIndex(
                name: "IX_AdventureBossUnlockFloor_AgentAddress",
                table: "AdventureBossUnlockFloor",
                column: "AgentAddress");

            migrationBuilder.CreateIndex(
                name: "IX_AdventureBossRush_AgentAddress",
                table: "AdventureBossRush",
                column: "AgentAddress");

            migrationBuilder.CreateIndex(
                name: "IX_AdventureBossClaimReward_AgentAddress",
                table: "AdventureBossClaimReward",
                column: "AgentAddress");

            migrationBuilder.CreateIndex(
                name: "IX_AdventureBossChallenge_AgentAddress",
                table: "AdventureBossChallenge",
                column: "AgentAddress");

            migrationBuilder.AddForeignKey(
                name: "FK_AdventureBossChallenge_Agents_AgentAddress",
                table: "AdventureBossChallenge",
                column: "AgentAddress",
                principalTable: "Agents",
                principalColumn: "Address");

            migrationBuilder.AddForeignKey(
                name: "FK_AdventureBossClaimReward_Agents_AgentAddress",
                table: "AdventureBossClaimReward",
                column: "AgentAddress",
                principalTable: "Agents",
                principalColumn: "Address");

            migrationBuilder.AddForeignKey(
                name: "FK_AdventureBossRush_Agents_AgentAddress",
                table: "AdventureBossRush",
                column: "AgentAddress",
                principalTable: "Agents",
                principalColumn: "Address");

            migrationBuilder.AddForeignKey(
                name: "FK_AdventureBossUnlockFloor_Agents_AgentAddress",
                table: "AdventureBossUnlockFloor",
                column: "AgentAddress",
                principalTable: "Agents",
                principalColumn: "Address");

            migrationBuilder.AddForeignKey(
                name: "FK_AdventureBossWanted_Agents_AgentAddress",
                table: "AdventureBossWanted",
                column: "AgentAddress",
                principalTable: "Agents",
                principalColumn: "Address");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdventureBossChallenge_Agents_AgentAddress",
                table: "AdventureBossChallenge");

            migrationBuilder.DropForeignKey(
                name: "FK_AdventureBossClaimReward_Agents_AgentAddress",
                table: "AdventureBossClaimReward");

            migrationBuilder.DropForeignKey(
                name: "FK_AdventureBossRush_Agents_AgentAddress",
                table: "AdventureBossRush");

            migrationBuilder.DropForeignKey(
                name: "FK_AdventureBossUnlockFloor_Agents_AgentAddress",
                table: "AdventureBossUnlockFloor");

            migrationBuilder.DropForeignKey(
                name: "FK_AdventureBossWanted_Agents_AgentAddress",
                table: "AdventureBossWanted");

            migrationBuilder.DropIndex(
                name: "IX_AdventureBossWanted_AgentAddress",
                table: "AdventureBossWanted");

            migrationBuilder.DropIndex(
                name: "IX_AdventureBossUnlockFloor_AgentAddress",
                table: "AdventureBossUnlockFloor");

            migrationBuilder.DropIndex(
                name: "IX_AdventureBossRush_AgentAddress",
                table: "AdventureBossRush");

            migrationBuilder.DropIndex(
                name: "IX_AdventureBossClaimReward_AgentAddress",
                table: "AdventureBossClaimReward");

            migrationBuilder.DropIndex(
                name: "IX_AdventureBossChallenge_AgentAddress",
                table: "AdventureBossChallenge");

            migrationBuilder.DropColumn(
                name: "AgentAddress",
                table: "AdventureBossWanted");

            migrationBuilder.DropColumn(
                name: "AgentAddress",
                table: "AdventureBossUnlockFloor");

            migrationBuilder.DropColumn(
                name: "AgentAddress",
                table: "AdventureBossRush");

            migrationBuilder.DropColumn(
                name: "AgentAddress",
                table: "AdventureBossClaimReward");

            migrationBuilder.DropColumn(
                name: "AgentAddress",
                table: "AdventureBossChallenge");
        }
    }
}
