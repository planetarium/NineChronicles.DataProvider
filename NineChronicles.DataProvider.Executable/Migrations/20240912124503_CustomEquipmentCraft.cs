using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NineChronicles.DataProvider.Executable.Migrations
{
    public partial class CustomEquipmentCraft : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AgentAddress",
                table: "CustomEquipmentCraft",
                type: "varchar(255)",
                nullable: false,
                defaultValue: string.Empty
            ).Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "Relationship",
                table: "CustomEquipmentCraft",
                type: "int",
                nullable: false
                );

            migrationBuilder.AddColumn<int>(
                name: "TotalCP",
                table: "CustomEquipmentCraft",
                type: "int",
                nullable: false
            );

            migrationBuilder.AddColumn<int>(
                name: "OptionId",
                table: "CustomEquipmentCraft",
                type: "int",
                nullable: false
                );

            migrationBuilder.AddColumn<bool>(
                name: "CraftWithRandom",
                table: "CustomEquipmentCraft",
                type: "tinyint(1)",
                nullable: false
                );

            migrationBuilder.AddColumn<bool>(
                name: "HasRandomOnlyIcon",
                table: "CustomEquipmentCraft",
                type: "tinyint(1)",
                nullable: false
                );

            migrationBuilder.RenameColumn(
                name: "DrawingAmount",
                table: "CustomEquipmentCraft",
                newName: "Scroll"
                );
            migrationBuilder.RenameColumn(
                name: "DrawingToolAmount",
                table: "CustomEquipmentCraft",
                newName: "Circle"
                );

            migrationBuilder.AddForeignKey(
                        name: "FK_CustomEquipmentCraft_Agents_AgentAddress",
                        table: "CustomEquipmentCraft",
                        column: "AgentAddress",
                        principalTable: "Agents",
                        principalColumn: "Address"
                );

            migrationBuilder.CreateIndex(
                name: "IX_CustomEquipmentCraft_AgentAddress",
                table: "CustomEquipmentCraft",
                column: "AgentAddress");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustomEquipmentCraft_AgentAddress",
                table: "CustomEquipmentCraft"
            );

            migrationBuilder.DropForeignKey(
                        name: "FK_CustomEquipmentCraft_Agents_AgentAddress",
                        table: "CustomEquipmentCraft"
            );

            migrationBuilder.RenameColumn(
                name: "Circle",
                table: "CustomEquipmentCraft",
                newName: "DrawingToolAmount"
            );
            migrationBuilder.RenameColumn(
                name: "Scroll",
                table: "CustomEquipmentCraft",
                newName: "DrawingAmount"
            );

            migrationBuilder.DropColumn(
                name: "HasRandomOnlyIcon",
                table: "CustomEquipmentCraft"
            );
            migrationBuilder.DropColumn(
                name: "CraftWithRandom",
                table: "CustomEquipmentCraft"
            );
            migrationBuilder.DropColumn(
                name: "OptionId",
                table: "CustomEquipmentCraft"
            );
            migrationBuilder.DropColumn(
                name: "TotalCP",
                table: "CustomEquipmentCraft"
            );
            migrationBuilder.DropColumn(
                name: "relationship",
                table: "CustomEquipmentCraft"
            );
            migrationBuilder.DropColumn(
                name: "AgentAddress",
                table: "CustomEquipmentCraft"
            );
        }
    }
}
