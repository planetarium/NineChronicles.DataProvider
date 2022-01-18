namespace NineChronicles.DataProvider.Executable.Migrations
{
    using Microsoft.EntityFrameworkCore.Migrations;

    public partial class AbilityRanking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "AgentAddress", table: "StageRankings", type: "TEXT", nullable: true);

            migrationBuilder.AddColumn<int>(name: "ArmorId", table: "StageRankings", type: "INTEGER", nullable: true);

            migrationBuilder.AddColumn<int>(name: "AvatarLevel", table: "StageRankings", type: "INTEGER", nullable: true);

            migrationBuilder.AddColumn<int>(name: "Cp", table: "StageRankings", type: "INTEGER", nullable: true);

            migrationBuilder.AddColumn<int>(name: "TitleId", table: "StageRankings", type: "INTEGER", nullable: true);

            migrationBuilder.AddColumn<int>(name: "ArmorId", table: "CraftRankings", type: "INTEGER", nullable: true);

            migrationBuilder.AddColumn<int>(name: "AvatarLevel", table: "CraftRankings", type: "INTEGER", nullable: true);

            migrationBuilder.AddColumn<int>(name: "Cp", table: "CraftRankings", type: "INTEGER", nullable: true);

            migrationBuilder.AddColumn<string>(name: "Name", table: "CraftRankings", type: "TEXT", nullable: true);

            migrationBuilder.AddColumn<int>(name: "TitleId", table: "CraftRankings", type: "INTEGER", nullable: true);

            migrationBuilder.AddColumn<int>(name: "ArmorId", table: "Avatars", type: "INTEGER", nullable: true);

            migrationBuilder.AddColumn<int>(name: "AvatarLevel", table: "Avatars", type: "INTEGER", nullable: true);

            migrationBuilder.AddColumn<int>(name: "Cp", table: "Avatars", type: "INTEGER", nullable: true);

            migrationBuilder.AddColumn<int>(name: "TitleId", table: "Avatars", type: "INTEGER", nullable: true);

            migrationBuilder.CreateTable(
                name: "AbilityRankings",
                columns: table => new
                {
                    AvatarAddress = table.Column<string>(type: "TEXT", nullable: false),
                    AgentAddress = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    AvatarLevel = table.Column<int>(type: "INTEGER", nullable: true),
                    TitleId = table.Column<int>(type: "INTEGER", nullable: true),
                    ArmorId = table.Column<int>(type: "INTEGER", nullable: true),
                    Cp = table.Column<int>(type: "INTEGER", nullable: true),
                    Ranking = table.Column<int>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbilityRankings", x => x.AvatarAddress);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentRankings",
                columns: table => new
                {
                    ItemId = table.Column<string>(type: "TEXT", nullable: false),
                    AgentAddress = table.Column<string>(type: "TEXT", nullable: true),
                    AvatarAddress = table.Column<string>(type: "TEXT", nullable: true),
                    EquipmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    Cp = table.Column<int>(type: "INTEGER", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemSubType = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    AvatarLevel = table.Column<int>(type: "INTEGER", nullable: true),
                    TitleId = table.Column<int>(type: "INTEGER", nullable: true),
                    ArmorId = table.Column<int>(type: "INTEGER", nullable: true),
                    Ranking = table.Column<int>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentRankings", x => x.ItemId);
                });

            migrationBuilder.CreateTable(
                name: "Equipments",
                columns: table => new
                {
                    ItemId = table.Column<string>(type: "TEXT", nullable: false),
                    AgentAddress = table.Column<string>(type: "TEXT", nullable: true),
                    AvatarAddress = table.Column<string>(type: "TEXT", nullable: true),
                    EquipmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    Cp = table.Column<int>(type: "INTEGER", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemSubType = table.Column<string>(type: "TEXT", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipments", x => x.ItemId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AbilityRankings");

            migrationBuilder.DropTable(
                name: "EquipmentRankings");

            migrationBuilder.DropTable(
                name: "Equipments");

            migrationBuilder.DropColumn(
                name: "AgentAddress",
                table: "StageRankings");

            migrationBuilder.DropColumn(
                name: "ArmorId",
                table: "StageRankings");

            migrationBuilder.DropColumn(
                name: "AvatarLevel",
                table: "StageRankings");

            migrationBuilder.DropColumn(
                name: "Cp",
                table: "StageRankings");

            migrationBuilder.DropColumn(
                name: "TitleId",
                table: "StageRankings");

            migrationBuilder.DropColumn(
                name: "ArmorId",
                table: "CraftRankings");

            migrationBuilder.DropColumn(
                name: "AvatarLevel",
                table: "CraftRankings");

            migrationBuilder.DropColumn(
                name: "Cp",
                table: "CraftRankings");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "CraftRankings");

            migrationBuilder.DropColumn(
                name: "TitleId",
                table: "CraftRankings");

            migrationBuilder.DropColumn(
                name: "ArmorId",
                table: "Avatars");

            migrationBuilder.DropColumn(
                name: "AvatarLevel",
                table: "Avatars");

            migrationBuilder.DropColumn(
                name: "Cp",
                table: "Avatars");

            migrationBuilder.DropColumn(
                name: "TitleId",
                table: "Avatars");
        }
    }
}
