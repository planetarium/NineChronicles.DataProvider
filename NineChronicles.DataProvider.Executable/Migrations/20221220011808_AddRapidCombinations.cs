using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NineChronicles.DataProvider.Executable.Migrations
{
    public partial class AddRapidCombinations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RapidCombinations",
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
                    HourglassCount = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RapidCombinations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RapidCombinations_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address");
                    table.ForeignKey(
                        name: "FK_RapidCombinations_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_RapidCombinations_AgentAddress",
                table: "RapidCombinations",
                column: "AgentAddress");

            migrationBuilder.CreateIndex(
                name: "IX_RapidCombinations_AvatarAddress",
                table: "RapidCombinations",
                column: "AvatarAddress");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RapidCombinations");
        }
    }
}
