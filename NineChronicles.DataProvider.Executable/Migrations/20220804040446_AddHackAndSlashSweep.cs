using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NineChronicles.DataProvider.Executable.Migrations
{
    public partial class AddHackAndSlashSweep : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "Timestamp",
                table: "Avatars",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HackAndSlashSweeps",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AgentAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    WorldId = table.Column<int>(type: "int", nullable: false),
                    StageId = table.Column<int>(type: "int", nullable: false),
                    ApStoneCount = table.Column<int>(type: "int", nullable: false),
                    ActionPoint = table.Column<int>(type: "int", nullable: false),
                    CostumesCount = table.Column<int>(type: "int", nullable: false),
                    EquipmentsCount = table.Column<int>(type: "int", nullable: false),
                    Cleared = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Mimisbrunnr = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HackAndSlashSweeps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HackAndSlashSweeps_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address");
                    table.ForeignKey(
                        name: "FK_HackAndSlashSweeps_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_HackAndSlashSweeps_AgentAddress",
                table: "HackAndSlashSweeps",
                column: "AgentAddress");

            migrationBuilder.CreateIndex(
                name: "IX_HackAndSlashSweeps_AvatarAddress",
                table: "HackAndSlashSweeps",
                column: "AvatarAddress");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HackAndSlashSweeps");

            migrationBuilder.DropColumn(
                name: "Timestamp",
                table: "Avatars");
        }
    }
}
