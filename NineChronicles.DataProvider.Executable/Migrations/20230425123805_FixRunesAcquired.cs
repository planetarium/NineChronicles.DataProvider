using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NineChronicles.DataProvider.Executable.Migrations
{
    public partial class FixRunesAcquired : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_RunesAcquired",
                table: "RunesAcquired");

            migrationBuilder.UpdateData(
                table: "RunesAcquired",
                keyColumn: "TickerType",
                keyValue: null,
                column: "TickerType",
                value: string.Empty);

            migrationBuilder.AlterColumn<string>(
                name: "TickerType",
                table: "RunesAcquired",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "RunesAcquired",
                keyColumn: "ActionType",
                keyValue: null,
                column: "ActionType",
                value: string.Empty);

            migrationBuilder.AlterColumn<string>(
                name: "ActionType",
                table: "RunesAcquired",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RunesAcquired",
                table: "RunesAcquired",
                columns: new[] { "Id", "ActionType", "TickerType" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_RunesAcquired",
                table: "RunesAcquired");

            migrationBuilder.AlterColumn<string>(
                name: "TickerType",
                table: "RunesAcquired",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "ActionType",
                table: "RunesAcquired",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RunesAcquired",
                table: "RunesAcquired",
                column: "Id");
        }
    }
}
