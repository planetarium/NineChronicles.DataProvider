namespace NineChronicles.DataProvider.Executable.Migrations
{
    using Microsoft.EntityFrameworkCore.Migrations;

    public partial class UpdateCraftRanking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateTable(
                name: "CraftRankingsOutput",
                columns: table => new
                {
                    AvatarAddress = table.Column<string>(type: "TEXT", nullable: false),
                    AgentAddress = table.Column<string>(type: "TEXT", nullable: true),
                    BlockIndex = table.Column<long>(type: "INTEGER", nullable: false),
                    CraftCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    AvatarLevel = table.Column<int>(type: "INTEGER", nullable: true),
                    TitleId = table.Column<int>(type: "INTEGER", nullable: true),
                    ArmorId = table.Column<int>(type: "INTEGER", nullable: true),
                    Cp = table.Column<int>(type: "INTEGER", nullable: true),
                    Ranking = table.Column<int>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CraftRankingsOutput", x => x.AvatarAddress);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CraftRankingsOutput");

            migrationBuilder.AddColumn<int>(
                name: "ArmorId",
                table: "CraftRankings",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AvatarLevel",
                table: "CraftRankings",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Cp",
                table: "CraftRankings",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "CraftRankings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TitleId",
                table: "CraftRankings",
                type: "INTEGER",
                nullable: true);
        }
    }
}
