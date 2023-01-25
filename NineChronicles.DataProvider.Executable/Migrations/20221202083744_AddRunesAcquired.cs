using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NineChronicles.DataProvider.Executable.Migrations
{
    public partial class AddRunesAcquired : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RunesAcquired",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ActionType = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TickerType = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    AgentAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AcquiredRune = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RunesAcquired", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RunesAcquired_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address");
                    table.ForeignKey(
                        name: "FK_RunesAcquired_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_RunesAcquired_AgentAddress",
                table: "RunesAcquired",
                column: "AgentAddress");

            migrationBuilder.CreateIndex(
                name: "IX_RunesAcquired_AvatarAddress",
                table: "RunesAcquired",
                column: "AvatarAddress");

            migrationBuilder.CreateIndex(
                name: "IX_RunesAcquired_Id_ActionType_TickerType",
                table: "RunesAcquired",
                columns: new[] { "Id", "ActionType", "TickerType" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RunesAcquired");
        }
    }
}
