using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NineChronicles.DataProvider.Executable.Migrations
{
    public partial class AddUserDataTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CraftRankingsOutput");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CraftRankings",
                table: "CraftRankings");

            migrationBuilder.AddColumn<DateOnly>(
                name: "Date",
                table: "UnlockWorlds",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "Date",
                table: "UnlockEquipmentRecipes",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "Date",
                table: "Stakings",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "Date",
                table: "ReplaceCombinationEquipmentMaterials",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "Date",
                table: "MigrateMonsterCollections",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "Date",
                table: "JoinArenas",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "Date",
                table: "ItemEnhancements",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TimeStamp",
                table: "ItemEnhancements",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateOnly>(
                name: "Date",
                table: "ItemEnhancementFails",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "Date",
                table: "HasWithRandomBuffs",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "Date",
                table: "HasRandomBuffs",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "Date",
                table: "HackAndSlashSweeps",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "Date",
                table: "HackAndSlashes",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "Timestamp",
                table: "HackAndSlashes",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateOnly>(
                name: "Date",
                table: "Grindings",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "Date",
                table: "EventDungeonBattles",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "Date",
                table: "EventConsumableItemCrafts",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TimeStamp",
                table: "Equipments",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AlterColumn<string>(
                name: "AvatarAddress",
                table: "CraftRankings",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "ArmorId",
                table: "CraftRankings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AvatarLevel",
                table: "CraftRankings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Cp",
                table: "CraftRankings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "CraftRankings",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "TitleId",
                table: "CraftRankings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "Date",
                table: "CombinationEquipments",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TimeStamp",
                table: "CombinationEquipments",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateOnly>(
                name: "Date",
                table: "CombinationConsumables",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TimeStamp",
                table: "CombinationConsumables",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateOnly>(
                name: "Date",
                table: "ClaimStakeRewards",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "Date",
                table: "Blocks",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "Date",
                table: "BattleArenas",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.CreateTable(
                name: "UserConsumables",
                columns: table => new
                {
                    BlockIndex = table.Column<long>(type: "bigint", nullable: true),
                    ItemId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AgentAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ItemType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ItemSubType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Id = table.Column<int>(type: "int", nullable: true),
                    BuffSkillCount = table.Column<int>(type: "int", nullable: true),
                    ElementalType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Grade = table.Column<int>(type: "int", nullable: true),
                    SkillsCount = table.Column<int>(type: "int", nullable: true),
                    RequiredBlockIndex = table.Column<long>(type: "bigint", nullable: true),
                    NonFungibleId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TradableId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MainStat = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TimeStamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_UserConsumables_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address");
                    table.ForeignKey(
                        name: "FK_UserConsumables_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserCostumes",
                columns: table => new
                {
                    BlockIndex = table.Column<long>(type: "bigint", nullable: true),
                    ItemId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AgentAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ItemType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ItemSubType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Id = table.Column<int>(type: "int", nullable: true),
                    ElementalType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Grade = table.Column<int>(type: "int", nullable: true),
                    Equipped = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    SpineResourcePath = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequiredBlockIndex = table.Column<long>(type: "bigint", nullable: true),
                    NonFungibleId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TradableId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TimeStamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_UserCostumes_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address");
                    table.ForeignKey(
                        name: "FK_UserCostumes_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserCrystals",
                columns: table => new
                {
                    BlockIndex = table.Column<long>(type: "bigint", nullable: true),
                    AgentAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CrystalBalance = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    TimeStamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_UserCrystals_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserEquipments",
                columns: table => new
                {
                    BlockIndex = table.Column<long>(type: "bigint", nullable: true),
                    ItemId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AgentAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ItemType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ItemSubType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Id = table.Column<int>(type: "int", nullable: true),
                    BuffSkillCount = table.Column<int>(type: "int", nullable: true),
                    ElementalType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Grade = table.Column<int>(type: "int", nullable: true),
                    Level = table.Column<int>(type: "int", nullable: true),
                    SetId = table.Column<int>(type: "int", nullable: true),
                    SkillsCount = table.Column<int>(type: "int", nullable: true),
                    SpineResourcePath = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequiredBlockIndex = table.Column<long>(type: "bigint", nullable: true),
                    NonFungibleId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TradableId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UniqueStatType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TimeStamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserMaterials",
                columns: table => new
                {
                    BlockIndex = table.Column<long>(type: "bigint", nullable: true),
                    ItemId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AgentAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ItemType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ItemSubType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Count = table.Column<int>(type: "int", nullable: true),
                    Id = table.Column<int>(type: "int", nullable: true),
                    ElementalType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Grade = table.Column<int>(type: "int", nullable: true),
                    TimeStamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_UserMaterials_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address");
                    table.ForeignKey(
                        name: "FK_UserMaterials_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserMonsterCollections",
                columns: table => new
                {
                    BlockIndex = table.Column<long>(type: "bigint", nullable: true),
                    AgentAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MonsterCollectionAmount = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    Level = table.Column<int>(type: "int", nullable: true),
                    RewardLevel = table.Column<long>(type: "bigint", nullable: true),
                    StartedBlockIndex = table.Column<long>(type: "bigint", nullable: true),
                    ReceivedBlockIndex = table.Column<long>(type: "bigint", nullable: true),
                    ExpiredBlockIndex = table.Column<long>(type: "bigint", nullable: true),
                    TimeStamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_UserMonsterCollections_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserNCGs",
                columns: table => new
                {
                    BlockIndex = table.Column<long>(type: "bigint", nullable: true),
                    AgentAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NCGBalance = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    TimeStamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_UserNCGs_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserRunes",
                columns: table => new
                {
                    BlockIndex = table.Column<long>(type: "bigint", nullable: true),
                    AgentAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Ticker = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RuneBalance = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    TimeStamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_UserRunes_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address");
                    table.ForeignKey(
                        name: "FK_UserRunes_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserStakings",
                columns: table => new
                {
                    BlockIndex = table.Column<long>(type: "bigint", nullable: true),
                    AgentAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StakeAmount = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    StartedBlockIndex = table.Column<long>(type: "bigint", nullable: true),
                    ReceivedBlockIndex = table.Column<long>(type: "bigint", nullable: true),
                    CancellableBlockIndex = table.Column<long>(type: "bigint", nullable: true),
                    TimeStamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_UserStakings_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_UserConsumables_AgentAddress",
                table: "UserConsumables",
                column: "AgentAddress");

            migrationBuilder.CreateIndex(
                name: "IX_UserConsumables_AvatarAddress",
                table: "UserConsumables",
                column: "AvatarAddress");

            migrationBuilder.CreateIndex(
                name: "IX_UserCostumes_AgentAddress",
                table: "UserCostumes",
                column: "AgentAddress");

            migrationBuilder.CreateIndex(
                name: "IX_UserCostumes_AvatarAddress",
                table: "UserCostumes",
                column: "AvatarAddress");

            migrationBuilder.CreateIndex(
                name: "IX_UserCrystals_AgentAddress",
                table: "UserCrystals",
                column: "AgentAddress");

            migrationBuilder.CreateIndex(
                name: "IX_UserMaterials_AgentAddress",
                table: "UserMaterials",
                column: "AgentAddress");

            migrationBuilder.CreateIndex(
                name: "IX_UserMaterials_AvatarAddress",
                table: "UserMaterials",
                column: "AvatarAddress");

            migrationBuilder.CreateIndex(
                name: "IX_UserMonsterCollections_AgentAddress",
                table: "UserMonsterCollections",
                column: "AgentAddress");

            migrationBuilder.CreateIndex(
                name: "IX_UserNCGs_AgentAddress",
                table: "UserNCGs",
                column: "AgentAddress");

            migrationBuilder.CreateIndex(
                name: "IX_UserRunes_AgentAddress",
                table: "UserRunes",
                column: "AgentAddress");

            migrationBuilder.CreateIndex(
                name: "IX_UserRunes_AvatarAddress",
                table: "UserRunes",
                column: "AvatarAddress");

            migrationBuilder.CreateIndex(
                name: "IX_UserStakings_AgentAddress",
                table: "UserStakings",
                column: "AgentAddress");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserConsumables");

            migrationBuilder.DropTable(
                name: "UserCostumes");

            migrationBuilder.DropTable(
                name: "UserCrystals");

            migrationBuilder.DropTable(
                name: "UserEquipments");

            migrationBuilder.DropTable(
                name: "UserMaterials");

            migrationBuilder.DropTable(
                name: "UserMonsterCollections");

            migrationBuilder.DropTable(
                name: "UserNCGs");

            migrationBuilder.DropTable(
                name: "UserRunes");

            migrationBuilder.DropTable(
                name: "UserStakings");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "UnlockWorlds");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "UnlockEquipmentRecipes");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "Stakings");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "ReplaceCombinationEquipmentMaterials");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "MigrateMonsterCollections");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "JoinArenas");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "ItemEnhancements");

            migrationBuilder.DropColumn(
                name: "TimeStamp",
                table: "ItemEnhancements");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "ItemEnhancementFails");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "HasWithRandomBuffs");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "HasRandomBuffs");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "HackAndSlashSweeps");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "HackAndSlashes");

            migrationBuilder.DropColumn(
                name: "Timestamp",
                table: "HackAndSlashes");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "Grindings");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "EventDungeonBattles");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "EventConsumableItemCrafts");

            migrationBuilder.DropColumn(
                name: "TimeStamp",
                table: "Equipments");

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
                name: "Date",
                table: "CombinationEquipments");

            migrationBuilder.DropColumn(
                name: "TimeStamp",
                table: "CombinationEquipments");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "CombinationConsumables");

            migrationBuilder.DropColumn(
                name: "TimeStamp",
                table: "CombinationConsumables");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "ClaimStakeRewards");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "Blocks");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "BattleArenas");

            migrationBuilder.UpdateData(
                table: "CraftRankings",
                keyColumn: "AvatarAddress",
                keyValue: null,
                column: "AvatarAddress",
                value: string.Empty);

            migrationBuilder.AlterColumn<string>(
                name: "AvatarAddress",
                table: "CraftRankings",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CraftRankings",
                table: "CraftRankings",
                column: "AvatarAddress");

            migrationBuilder.CreateTable(
                name: "CraftRankingsOutput",
                columns: table => new
                {
                    AgentAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ArmorId = table.Column<int>(type: "int", nullable: true),
                    AvatarAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarLevel = table.Column<int>(type: "int", nullable: true),
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    Cp = table.Column<int>(type: "int", nullable: true),
                    CraftCount = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Ranking = table.Column<int>(type: "int", nullable: false),
                    TitleId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
