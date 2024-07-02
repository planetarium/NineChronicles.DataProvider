using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NineChronicles.DataProvider.Executable.Migrations
{
    public partial class AdventureBoss : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdventureBossChallenge",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    AvatarAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StartFloor = table.Column<int>(type: "int", nullable: false),
                    EndFloor = table.Column<int>(type: "int", nullable: false),
                    UsedApPotion = table.Column<int>(type: "int", nullable: false),
                    Point = table.Column<int>(type: "int", nullable: false),
                    TotalPoint = table.Column<long>(type: "bigint", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdventureBossChallenge", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdventureBossChallenge_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AdventureBossClaimReward",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    ClaimedSeason = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NcgReward = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    RewardData = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdventureBossClaimReward", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdventureBossClaimReward_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AdventureBossRush",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    Season = table.Column<int>(type: "int", nullable: false),
                    AvatarAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EndFloor = table.Column<int>(type: "int", nullable: false),
                    UsedApPotion = table.Column<int>(type: "int", nullable: false),
                    Point = table.Column<int>(type: "int", nullable: false),
                    TotalPoint = table.Column<long>(type: "bigint", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdventureBossRush", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdventureBossRush_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AdventureBossSeason",
                columns: table => new
                {
                    Season = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StartBlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    EndBlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    ClaimableBlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    NextSeasonBlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    BossId = table.Column<int>(type: "int", nullable: false),
                    FixedRewardData = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RandomRewardData = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RaffleWinnerAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RaffleReward = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdventureBossSeason", x => x.Season);
                    table.ForeignKey(
                        name: "FK_AdventureBossSeason_Avatars_RaffleWinnerAddress",
                        column: x => x.RaffleWinnerAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AdventureBossUnlockFloor",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    Season = table.Column<long>(type: "bigint", nullable: false),
                    AvatarAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UnlockFloor = table.Column<int>(type: "int", nullable: false),
                    UsedGoldenDust = table.Column<long>(type: "bigint", nullable: false),
                    UsedNcg = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    TotalUsedGoldenDust = table.Column<long>(type: "bigint", nullable: false),
                    TotalUsedNcg = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdventureBossUnlockFloor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdventureBossUnlockFloor_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AdventureBossWanted",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    Season = table.Column<int>(type: "int", nullable: false),
                    AvatarAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Bounty = table.Column<int>(type: "int", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false),
                    TotalBounty = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdventureBossWanted", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdventureBossWanted_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AdventureBossChallenge_AvatarAddress",
                table: "AdventureBossChallenge",
                column: "AvatarAddress");

            migrationBuilder.CreateIndex(
                name: "IX_AdventureBossClaimReward_AvatarAddress",
                table: "AdventureBossClaimReward",
                column: "AvatarAddress");

            migrationBuilder.CreateIndex(
                name: "IX_AdventureBossRush_AvatarAddress",
                table: "AdventureBossRush",
                column: "AvatarAddress");

            migrationBuilder.CreateIndex(
                name: "IX_AdventureBossSeason_RaffleWinnerAddress",
                table: "AdventureBossSeason",
                column: "RaffleWinnerAddress");

            migrationBuilder.CreateIndex(
                name: "IX_AdventureBossUnlockFloor_AvatarAddress",
                table: "AdventureBossUnlockFloor",
                column: "AvatarAddress");

            migrationBuilder.CreateIndex(
                name: "IX_AdventureBossWanted_AvatarAddress",
                table: "AdventureBossWanted",
                column: "AvatarAddress");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdventureBossChallenge");

            migrationBuilder.DropTable(
                name: "AdventureBossClaimReward");

            migrationBuilder.DropTable(
                name: "AdventureBossRush");

            migrationBuilder.DropTable(
                name: "AdventureBossSeason");

            migrationBuilder.DropTable(
                name: "AdventureBossUnlockFloor");

            migrationBuilder.DropTable(
                name: "AdventureBossWanted");
        }
    }
}
