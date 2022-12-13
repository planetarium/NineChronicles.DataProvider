using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NineChronicles.DataProvider.Executable.Migrations
{
    public partial class AddRuneEnhancements : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RuneEnhancements",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    AgentAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PreviousRuneLevel = table.Column<int>(type: "int", nullable: false),
                    OutputRuneLevel = table.Column<int>(type: "int", nullable: false),
                    RuneId = table.Column<int>(type: "int", nullable: false),
                    TryCount = table.Column<int>(type: "int", nullable: false),
                    BurntNCG = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    BurntCrystal = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    BurntRune = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Date = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuneEnhancements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuneEnhancements_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address");
                    table.ForeignKey(
                        name: "FK_RuneEnhancements_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_RuneEnhancements_AgentAddress",
                table: "RuneEnhancements",
                column: "AgentAddress");

            migrationBuilder.CreateIndex(
                name: "IX_RuneEnhancements_AvatarAddress",
                table: "RuneEnhancements",
                column: "AvatarAddress");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RuneEnhancements");
        }
    }
}
