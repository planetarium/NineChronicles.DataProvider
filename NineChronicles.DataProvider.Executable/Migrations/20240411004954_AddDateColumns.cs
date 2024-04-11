using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NineChronicles.DataProvider.Executable.Migrations
{
    public partial class AddDateColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "Date",
                table: "ShopHistoryMaterials",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "Date",
                table: "ShopHistoryFungibleAssetValues",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "Date",
                table: "ShopHistoryEquipments",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "Date",
                table: "ShopHistoryCostumes",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "Date",
                table: "ShopHistoryConsumables",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "Date",
                table: "RuneSummons",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TimeStamp",
                table: "RuneSummons",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateOnly>(
                name: "Date",
                table: "RuneSummonFails",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TimeStamp",
                table: "RuneSummonFails",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateOnly>(
                name: "Date",
                table: "AuraSummons",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TimeStamp",
                table: "AuraSummons",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateOnly>(
                name: "Date",
                table: "AuraSummonFails",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TimeStamp",
                table: "AuraSummonFails",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateIndex(
                name: "IX_ShopHistoryMaterials_Date",
                table: "ShopHistoryMaterials",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_ShopHistoryFungibleAssetValues_Date",
                table: "ShopHistoryFungibleAssetValues",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_ShopHistoryEquipments_Date",
                table: "ShopHistoryEquipments",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_ShopHistoryCostumes_Date",
                table: "ShopHistoryCostumes",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_ShopHistoryConsumables_Date",
                table: "ShopHistoryConsumables",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_RuneSummons_Date",
                table: "RuneSummons",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_RuneSummonFails_Date",
                table: "RuneSummonFails",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_AuraSummons_Date",
                table: "AuraSummons",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_AuraSummonFails_Date",
                table: "AuraSummonFails",
                column: "Date");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShopHistoryMaterials_Date",
                table: "ShopHistoryMaterials");

            migrationBuilder.DropIndex(
                name: "IX_ShopHistoryFungibleAssetValues_Date",
                table: "ShopHistoryFungibleAssetValues");

            migrationBuilder.DropIndex(
                name: "IX_ShopHistoryEquipments_Date",
                table: "ShopHistoryEquipments");

            migrationBuilder.DropIndex(
                name: "IX_ShopHistoryCostumes_Date",
                table: "ShopHistoryCostumes");

            migrationBuilder.DropIndex(
                name: "IX_ShopHistoryConsumables_Date",
                table: "ShopHistoryConsumables");

            migrationBuilder.DropIndex(
                name: "IX_RuneSummons_Date",
                table: "RuneSummons");

            migrationBuilder.DropIndex(
                name: "IX_RuneSummonFails_Date",
                table: "RuneSummonFails");

            migrationBuilder.DropIndex(
                name: "IX_AuraSummons_Date",
                table: "AuraSummons");

            migrationBuilder.DropIndex(
                name: "IX_AuraSummonFails_Date",
                table: "AuraSummonFails");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "ShopHistoryMaterials");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "ShopHistoryFungibleAssetValues");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "ShopHistoryEquipments");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "ShopHistoryCostumes");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "ShopHistoryConsumables");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "RuneSummons");

            migrationBuilder.DropColumn(
                name: "TimeStamp",
                table: "RuneSummons");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "RuneSummonFails");

            migrationBuilder.DropColumn(
                name: "TimeStamp",
                table: "RuneSummonFails");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "AuraSummons");

            migrationBuilder.DropColumn(
                name: "TimeStamp",
                table: "AuraSummons");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "AuraSummonFails");

            migrationBuilder.DropColumn(
                name: "TimeStamp",
                table: "AuraSummonFails");
        }
    }
}
