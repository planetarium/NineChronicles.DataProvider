using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NineChronicles.DataProvider.Executable.Migrations
{
    public partial class AddSynthesize : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CostumeSummons",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GroupId = table.Column<int>(type: "int", nullable: false),
                    SummonCount = table.Column<int>(type: "int", nullable: false),
                    SummonResult = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    AgentAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostumeSummons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostumeSummons_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address");
                    table.ForeignKey(
                        name: "FK_CostumeSummons_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Synthesizes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaterialGradeId = table.Column<int>(type: "int", nullable: false),
                    MaterialItemSubTypeId = table.Column<int>(type: "int", nullable: false),
                    MaterialInfo = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResultInfo = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    AgentAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Synthesizes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Synthesizes_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address");
                    table.ForeignKey(
                        name: "FK_Synthesizes_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CostumeSummons_AgentAddress",
                table: "CostumeSummons",
                column: "AgentAddress");

            migrationBuilder.CreateIndex(
                name: "IX_CostumeSummons_AvatarAddress",
                table: "CostumeSummons",
                column: "AvatarAddress");

            migrationBuilder.CreateIndex(
                name: "IX_CostumeSummons_Date",
                table: "CostumeSummons",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_Synthesizes_AgentAddress",
                table: "Synthesizes",
                column: "AgentAddress");

            migrationBuilder.CreateIndex(
                name: "IX_Synthesizes_AvatarAddress",
                table: "Synthesizes",
                column: "AvatarAddress");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CostumeSummons");

            migrationBuilder.DropTable(
                name: "Synthesizes");
        }
    }
}
