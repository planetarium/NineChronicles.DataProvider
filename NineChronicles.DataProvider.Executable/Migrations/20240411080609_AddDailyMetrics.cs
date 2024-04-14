using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NineChronicles.DataProvider.Executable.Migrations
{
    public partial class AddDailyMetrics : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ActionType",
                table: "Transactions",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DailyMetrics",
                columns: table => new
                {
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Dau = table.Column<int>(type: "int", nullable: true),
                    TxCount = table.Column<int>(type: "int", nullable: true),
                    DailyNew = table.Column<int>(type: "int", nullable: true),
                    HackAndSlashCount = table.Column<int>(type: "int", nullable: true),
                    HackAndSlashUsers = table.Column<int>(type: "int", nullable: true),
                    SweepCount = table.Column<int>(type: "int", nullable: true),
                    SweepUsers = table.Column<int>(type: "int", nullable: true),
                    CraftingEquipmentCount = table.Column<int>(type: "int", nullable: true),
                    CraftingEquipmentUsers = table.Column<int>(type: "int", nullable: true),
                    CraftingConsumableCount = table.Column<int>(type: "int", nullable: true),
                    CraftingConsumableUsers = table.Column<int>(type: "int", nullable: true),
                    EnhanceCount = table.Column<int>(type: "int", nullable: true),
                    EnhanceUsers = table.Column<int>(type: "int", nullable: true),
                    AuraSummonCount = table.Column<int>(type: "int", nullable: true),
                    RuneSummonCount = table.Column<int>(type: "int", nullable: true),
                    ApUsage = table.Column<int>(type: "int", nullable: true),
                    HourglassUsage = table.Column<int>(type: "int", nullable: true),
                    NcgTrade = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    EnhanceNcg = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    RuneNcg = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    RuneSlotNcg = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    ArenaNcg = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    EventTicketNcg = table.Column<decimal>(type: "decimal(65,30)", nullable: true)
                },
                constraints: table =>
                {
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IDX_Date_ActionType",
                table: "Transactions",
                columns: new[] { "Date", "ActionType" });

            migrationBuilder.CreateIndex(
                name: "IX_DailyMetrics_Date",
                table: "DailyMetrics",
                column: "Date");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyMetrics");

            migrationBuilder.DropIndex(
                name: "IDX_Date_ActionType",
                table: "Transactions");

            migrationBuilder.AlterColumn<string>(
                name: "ActionType",
                table: "Transactions",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
