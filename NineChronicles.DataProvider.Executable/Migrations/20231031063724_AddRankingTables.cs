using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NineChronicles.DataProvider.Executable.Migrations
{
    public partial class AddRankingTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EquipmentRankingArmor",
                columns: table => new
                {
                    ItemId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AgentAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EquipmentId = table.Column<int>(type: "int", nullable: false),
                    Cp = table.Column<int>(type: "int", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    ItemSubType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarLevel = table.Column<int>(type: "int", nullable: true),
                    TitleId = table.Column<int>(type: "int", nullable: true),
                    ArmorId = table.Column<int>(type: "int", nullable: true),
                    Ranking = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EquipmentRankingBelt",
                columns: table => new
                {
                    ItemId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AgentAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EquipmentId = table.Column<int>(type: "int", nullable: false),
                    Cp = table.Column<int>(type: "int", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    ItemSubType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarLevel = table.Column<int>(type: "int", nullable: true),
                    TitleId = table.Column<int>(type: "int", nullable: true),
                    ArmorId = table.Column<int>(type: "int", nullable: true),
                    Ranking = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EquipmentRankingNecklace",
                columns: table => new
                {
                    ItemId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AgentAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EquipmentId = table.Column<int>(type: "int", nullable: false),
                    Cp = table.Column<int>(type: "int", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    ItemSubType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarLevel = table.Column<int>(type: "int", nullable: true),
                    TitleId = table.Column<int>(type: "int", nullable: true),
                    ArmorId = table.Column<int>(type: "int", nullable: true),
                    Ranking = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EquipmentRankingRing",
                columns: table => new
                {
                    ItemId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AgentAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EquipmentId = table.Column<int>(type: "int", nullable: false),
                    Cp = table.Column<int>(type: "int", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    ItemSubType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarLevel = table.Column<int>(type: "int", nullable: true),
                    TitleId = table.Column<int>(type: "int", nullable: true),
                    ArmorId = table.Column<int>(type: "int", nullable: true),
                    Ranking = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EquipmentRankingWeapon",
                columns: table => new
                {
                    ItemId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AgentAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EquipmentId = table.Column<int>(type: "int", nullable: false),
                    Cp = table.Column<int>(type: "int", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    ItemSubType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarLevel = table.Column<int>(type: "int", nullable: true),
                    TitleId = table.Column<int>(type: "int", nullable: true),
                    ArmorId = table.Column<int>(type: "int", nullable: true),
                    Ranking = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EquipmentRankingArmor");

            migrationBuilder.DropTable(
                name: "EquipmentRankingBelt");

            migrationBuilder.DropTable(
                name: "EquipmentRankingNecklace");

            migrationBuilder.DropTable(
                name: "EquipmentRankingRing");

            migrationBuilder.DropTable(
                name: "EquipmentRankingWeapon");
        }
    }
}
