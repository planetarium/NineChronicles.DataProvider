namespace NineChronicles.DataProvider.Executable.Migrations
{
    using System;
    using Microsoft.EntityFrameworkCore.Migrations;

    public partial class AddCrystalStakingBattleArena : Migration
    {
#pragma warning disable MEN003
        protected override void Up(MigrationBuilder migrationBuilder)
#pragma warning restore MEN003
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_StageRanking",
                table: "StageRanking");

            migrationBuilder.AlterColumn<string>(
                name: "AvatarAddress",
                table: "StageRanking",
                type: "TEXT",
                nullable: false,
                defaultValue: string.Empty,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Ranking",
                table: "StageRanking",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "TimeStamp",
                table: "ShopHistoryMaterials",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "TimeStamp",
                table: "ShopHistoryEquipments",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "TimeStamp",
                table: "ShopHistoryCostumes",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "TimeStamp",
                table: "ShopHistoryConsumables",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_StageRanking",
                table: "StageRanking",
                column: "AvatarAddress");

            migrationBuilder.CreateTable(
                name: "BattleArenaRanking",
                columns: table => new
                {
                    AvatarAddress = table.Column<string>(type: "TEXT", nullable: false),
                    BlockIndex = table.Column<long>(type: "INTEGER", nullable: false),
                    AgentAddress = table.Column<string>(type: "TEXT", nullable: true),
                    AvatarLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    ChampionshipId = table.Column<int>(type: "INTEGER", nullable: false),
                    Round = table.Column<int>(type: "INTEGER", nullable: false),
                    ArenaType = table.Column<string>(type: "TEXT", nullable: true),
                    Score = table.Column<int>(type: "INTEGER", nullable: false),
                    WinCount = table.Column<int>(type: "INTEGER", nullable: false),
                    MedalCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LossCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Ticket = table.Column<int>(type: "INTEGER", nullable: false),
                    PurchasedTicketCount = table.Column<int>(type: "INTEGER", nullable: false),
                    TicketResetCount = table.Column<int>(type: "INTEGER", nullable: false),
                    EntranceFee = table.Column<long>(type: "INTEGER", nullable: false),
                    TicketPrice = table.Column<long>(type: "INTEGER", nullable: false),
                    AdditionalTicketPrice = table.Column<long>(type: "INTEGER", nullable: false),
                    RequiredMedalCount = table.Column<int>(type: "INTEGER", nullable: false),
                    StartBlockIndex = table.Column<long>(type: "INTEGER", nullable: false),
                    EndBlockIndex = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    TitleId = table.Column<int>(type: "INTEGER", nullable: true),
                    ArmorId = table.Column<int>(type: "INTEGER", nullable: true),
                    Cp = table.Column<int>(type: "INTEGER", nullable: true),
                    Ranking = table.Column<int>(type: "INTEGER", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BattleArenaRanking", x => x.AvatarAddress);
                });

            migrationBuilder.CreateTable(
                name: "BattleArenas",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    BlockIndex = table.Column<long>(type: "INTEGER", nullable: false),
                    AgentAddress = table.Column<string>(type: "TEXT", nullable: true),
                    AvatarAddress = table.Column<string>(type: "TEXT", nullable: true),
                    AvatarLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    EnemyAvatarAddress = table.Column<string>(type: "TEXT", nullable: true),
                    ChampionshipId = table.Column<int>(type: "INTEGER", nullable: false),
                    Round = table.Column<int>(type: "INTEGER", nullable: false),
                    TicketCount = table.Column<int>(type: "INTEGER", nullable: false),
                    BurntNCG = table.Column<decimal>(type: "TEXT", nullable: false),
                    Victory = table.Column<bool>(type: "INTEGER", nullable: false),
                    MedalCount = table.Column<int>(type: "INTEGER", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BattleArenas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BattleArenas_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BattleArenas_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClaimStakeRewards",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    BlockIndex = table.Column<long>(type: "INTEGER", nullable: false),
                    AgentAddress = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimRewardAvatarAddress = table.Column<string>(type: "TEXT", nullable: true),
                    HourGlassCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ApPotionCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ClaimStakeStartBlockIndex = table.Column<long>(type: "INTEGER", nullable: false),
                    ClaimStakeEndBlockIndex = table.Column<long>(type: "INTEGER", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimStakeRewards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimStakeRewards_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Grindings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    BlockIndex = table.Column<long>(type: "INTEGER", nullable: false),
                    AgentAddress = table.Column<string>(type: "TEXT", nullable: true),
                    AvatarAddress = table.Column<string>(type: "TEXT", nullable: true),
                    EquipmentItemId = table.Column<string>(type: "TEXT", nullable: true),
                    EquipmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    EquipmentLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    Crystal = table.Column<decimal>(type: "TEXT", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Grindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Grindings_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Grindings_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HasRandomBuffs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    BlockIndex = table.Column<long>(type: "INTEGER", nullable: false),
                    AgentAddress = table.Column<string>(type: "TEXT", nullable: true),
                    AvatarAddress = table.Column<string>(type: "TEXT", nullable: true),
                    HasStageId = table.Column<int>(type: "INTEGER", nullable: false),
                    GachaCount = table.Column<int>(type: "INTEGER", nullable: false),
                    BurntCrystal = table.Column<decimal>(type: "TEXT", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HasRandomBuffs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HasRandomBuffs_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HasRandomBuffs_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HasWithRandomBuffs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    BlockIndex = table.Column<long>(type: "INTEGER", nullable: false),
                    AgentAddress = table.Column<string>(type: "TEXT", nullable: true),
                    AvatarAddress = table.Column<string>(type: "TEXT", nullable: true),
                    StageId = table.Column<int>(type: "INTEGER", nullable: false),
                    BuffId = table.Column<int>(type: "INTEGER", nullable: false),
                    Cleared = table.Column<bool>(type: "INTEGER", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HasWithRandomBuffs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HasWithRandomBuffs_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HasWithRandomBuffs_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItemEnhancementFails",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    BlockIndex = table.Column<long>(type: "INTEGER", nullable: false),
                    AgentAddress = table.Column<string>(type: "TEXT", nullable: true),
                    AvatarAddress = table.Column<string>(type: "TEXT", nullable: true),
                    EquipmentItemId = table.Column<string>(type: "TEXT", nullable: true),
                    MaterialItemId = table.Column<string>(type: "TEXT", nullable: true),
                    EquipmentLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    GainedCrystal = table.Column<decimal>(type: "TEXT", nullable: false),
                    BurntNCG = table.Column<decimal>(type: "TEXT", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemEnhancementFails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemEnhancementFails_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItemEnhancementFails_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JoinArenas",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    BlockIndex = table.Column<long>(type: "INTEGER", nullable: false),
                    AgentAddress = table.Column<string>(type: "TEXT", nullable: true),
                    AvatarAddress = table.Column<string>(type: "TEXT", nullable: true),
                    AvatarLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    ArenaRound = table.Column<int>(type: "INTEGER", nullable: false),
                    ChampionshipId = table.Column<int>(type: "INTEGER", nullable: false),
                    BurntCrystal = table.Column<decimal>(type: "TEXT", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JoinArenas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JoinArenas_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JoinArenas_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MigrateMonsterCollections",
                columns: table => new
                {
                    AgentAddress = table.Column<string>(type: "TEXT", nullable: false),
                    BlockIndex = table.Column<long>(type: "INTEGER", nullable: false),
                    AgentAddress1 = table.Column<string>(type: "TEXT", nullable: false),
                    MigrationAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    MigrationStartBlockIndex = table.Column<long>(type: "INTEGER", nullable: false),
                    StakeStartBlockIndex = table.Column<long>(type: "INTEGER", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MigrateMonsterCollections", x => x.AgentAddress);
                    table.ForeignKey(
                        name: "FK_MigrateMonsterCollections_Agents_AgentAddress1",
                        column: x => x.AgentAddress1,
                        principalTable: "Agents",
                        principalColumn: "Address",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReplaceCombinationEquipmentMaterials",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    BlockIndex = table.Column<long>(type: "INTEGER", nullable: false),
                    AgentAddress = table.Column<string>(type: "TEXT", nullable: true),
                    AvatarAddress = table.Column<string>(type: "TEXT", nullable: true),
                    ReplacedMaterialId = table.Column<int>(type: "INTEGER", nullable: false),
                    ReplacedMaterialCount = table.Column<int>(type: "INTEGER", nullable: false),
                    BurntCrystal = table.Column<decimal>(type: "TEXT", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplaceCombinationEquipmentMaterials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReplaceCombinationEquipmentMaterials_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReplaceCombinationEquipmentMaterials_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShopConsumables",
                columns: table => new
                {
                    ItemId = table.Column<string>(type: "TEXT", nullable: false),
                    BlockIndex = table.Column<long>(type: "INTEGER", nullable: false),
                    SellerAgentAddress = table.Column<string>(type: "TEXT", nullable: true),
                    SellerAvatarAddress = table.Column<string>(type: "TEXT", nullable: true),
                    ItemType = table.Column<string>(type: "TEXT", nullable: true),
                    ItemSubType = table.Column<string>(type: "TEXT", nullable: true),
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    BuffSkillCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ElementalType = table.Column<string>(type: "TEXT", nullable: true),
                    Grade = table.Column<int>(type: "INTEGER", nullable: false),
                    SkillsCount = table.Column<int>(type: "INTEGER", nullable: false),
                    RequiredBlockIndex = table.Column<long>(type: "INTEGER", nullable: false),
                    NonFungibleId = table.Column<string>(type: "TEXT", nullable: true),
                    TradableId = table.Column<string>(type: "TEXT", nullable: true),
                    MainStat = table.Column<string>(type: "TEXT", nullable: true),
                    Price = table.Column<decimal>(type: "TEXT", nullable: false),
                    OrderId = table.Column<string>(type: "TEXT", nullable: true),
                    CombatPoint = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SellStartedBlockIndex = table.Column<long>(type: "INTEGER", nullable: false),
                    SellExpiredBlockIndex = table.Column<long>(type: "INTEGER", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopConsumables", x => x.ItemId);
                });

            migrationBuilder.CreateTable(
                name: "ShopCostumes",
                columns: table => new
                {
                    ItemId = table.Column<string>(type: "TEXT", nullable: false),
                    BlockIndex = table.Column<long>(type: "INTEGER", nullable: false),
                    SellerAgentAddress = table.Column<string>(type: "TEXT", nullable: true),
                    SellerAvatarAddress = table.Column<string>(type: "TEXT", nullable: true),
                    ItemType = table.Column<string>(type: "TEXT", nullable: true),
                    ItemSubType = table.Column<string>(type: "TEXT", nullable: true),
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    ElementalType = table.Column<string>(type: "TEXT", nullable: true),
                    Grade = table.Column<int>(type: "INTEGER", nullable: false),
                    Equipped = table.Column<bool>(type: "INTEGER", nullable: false),
                    SpineResourcePath = table.Column<string>(type: "TEXT", nullable: true),
                    RequiredBlockIndex = table.Column<long>(type: "INTEGER", nullable: false),
                    NonFungibleId = table.Column<string>(type: "TEXT", nullable: true),
                    TradableId = table.Column<string>(type: "TEXT", nullable: true),
                    Price = table.Column<decimal>(type: "TEXT", nullable: false),
                    OrderId = table.Column<string>(type: "TEXT", nullable: true),
                    CombatPoint = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SellStartedBlockIndex = table.Column<long>(type: "INTEGER", nullable: false),
                    SellExpiredBlockIndex = table.Column<long>(type: "INTEGER", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopCostumes", x => x.ItemId);
                });

            migrationBuilder.CreateTable(
                name: "ShopEquipments",
                columns: table => new
                {
                    ItemId = table.Column<string>(type: "TEXT", nullable: false),
                    BlockIndex = table.Column<long>(type: "INTEGER", nullable: false),
                    SellerAgentAddress = table.Column<string>(type: "TEXT", nullable: true),
                    SellerAvatarAddress = table.Column<string>(type: "TEXT", nullable: true),
                    ItemType = table.Column<string>(type: "TEXT", nullable: true),
                    ItemSubType = table.Column<string>(type: "TEXT", nullable: true),
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    BuffSkillCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ElementalType = table.Column<string>(type: "TEXT", nullable: true),
                    Grade = table.Column<int>(type: "INTEGER", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    SetId = table.Column<int>(type: "INTEGER", nullable: false),
                    SkillsCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SpineResourcePath = table.Column<string>(type: "TEXT", nullable: true),
                    RequiredBlockIndex = table.Column<long>(type: "INTEGER", nullable: false),
                    NonFungibleId = table.Column<string>(type: "TEXT", nullable: true),
                    TradableId = table.Column<string>(type: "TEXT", nullable: true),
                    UniqueStatType = table.Column<string>(type: "TEXT", nullable: true),
                    Price = table.Column<decimal>(type: "TEXT", nullable: false),
                    OrderId = table.Column<string>(type: "TEXT", nullable: true),
                    CombatPoint = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SellStartedBlockIndex = table.Column<long>(type: "INTEGER", nullable: false),
                    SellExpiredBlockIndex = table.Column<long>(type: "INTEGER", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopEquipments", x => x.ItemId);
                });

            migrationBuilder.CreateTable(
                name: "ShopMaterials",
                columns: table => new
                {
                    ItemId = table.Column<string>(type: "TEXT", nullable: false),
                    BlockIndex = table.Column<long>(type: "INTEGER", nullable: false),
                    SellerAgentAddress = table.Column<string>(type: "TEXT", nullable: true),
                    SellerAvatarAddress = table.Column<string>(type: "TEXT", nullable: true),
                    ItemType = table.Column<string>(type: "TEXT", nullable: true),
                    ItemSubType = table.Column<string>(type: "TEXT", nullable: true),
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    ElementalType = table.Column<string>(type: "TEXT", nullable: true),
                    Grade = table.Column<int>(type: "INTEGER", nullable: false),
                    TradableId = table.Column<string>(type: "TEXT", nullable: true),
                    Price = table.Column<decimal>(type: "TEXT", nullable: false),
                    OrderId = table.Column<string>(type: "TEXT", nullable: true),
                    CombatPoint = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SellStartedBlockIndex = table.Column<long>(type: "INTEGER", nullable: false),
                    SellExpiredBlockIndex = table.Column<long>(type: "INTEGER", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopMaterials", x => x.ItemId);
                });

            migrationBuilder.CreateTable(
                name: "Stakings",
                columns: table => new
                {
                    TimeStamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    BlockIndex = table.Column<long>(type: "INTEGER", nullable: false),
                    AgentAddress = table.Column<string>(type: "TEXT", nullable: true),
                    PreviousAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    NewAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    RemainingNCG = table.Column<decimal>(type: "TEXT", nullable: false),
                    PrevStakeStartBlockIndex = table.Column<long>(type: "INTEGER", nullable: false),
                    NewStakeStartBlockIndex = table.Column<long>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stakings", x => x.TimeStamp);
                    table.ForeignKey(
                        name: "FK_Stakings_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UnlockEquipmentRecipes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    BlockIndex = table.Column<long>(type: "INTEGER", nullable: false),
                    AgentAddress = table.Column<string>(type: "TEXT", nullable: true),
                    AvatarAddress = table.Column<string>(type: "TEXT", nullable: true),
                    UnlockEquipmentRecipeId = table.Column<int>(type: "INTEGER", nullable: false),
                    BurntCrystal = table.Column<decimal>(type: "TEXT", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnlockEquipmentRecipes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnlockEquipmentRecipes_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UnlockEquipmentRecipes_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UnlockWorlds",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    BlockIndex = table.Column<long>(type: "INTEGER", nullable: false),
                    AgentAddress = table.Column<string>(type: "TEXT", nullable: true),
                    AvatarAddress = table.Column<string>(type: "TEXT", nullable: true),
                    UnlockWorldId = table.Column<int>(type: "INTEGER", nullable: false),
                    BurntCrystal = table.Column<decimal>(type: "TEXT", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnlockWorlds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnlockWorlds_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UnlockWorlds_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BattleArenas_AgentAddress",
                table: "BattleArenas",
                column: "AgentAddress");

            migrationBuilder.CreateIndex(
                name: "IX_BattleArenas_AvatarAddress",
                table: "BattleArenas",
                column: "AvatarAddress");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimStakeRewards_AgentAddress",
                table: "ClaimStakeRewards",
                column: "AgentAddress");

            migrationBuilder.CreateIndex(
                name: "IX_Grindings_AgentAddress",
                table: "Grindings",
                column: "AgentAddress");

            migrationBuilder.CreateIndex(
                name: "IX_Grindings_AvatarAddress",
                table: "Grindings",
                column: "AvatarAddress");

            migrationBuilder.CreateIndex(
                name: "IX_HasRandomBuffs_AgentAddress",
                table: "HasRandomBuffs",
                column: "AgentAddress");

            migrationBuilder.CreateIndex(
                name: "IX_HasRandomBuffs_AvatarAddress",
                table: "HasRandomBuffs",
                column: "AvatarAddress");

            migrationBuilder.CreateIndex(
                name: "IX_HasWithRandomBuffs_AgentAddress",
                table: "HasWithRandomBuffs",
                column: "AgentAddress");

            migrationBuilder.CreateIndex(
                name: "IX_HasWithRandomBuffs_AvatarAddress",
                table: "HasWithRandomBuffs",
                column: "AvatarAddress");

            migrationBuilder.CreateIndex(
                name: "IX_ItemEnhancementFails_AgentAddress",
                table: "ItemEnhancementFails",
                column: "AgentAddress");

            migrationBuilder.CreateIndex(
                name: "IX_ItemEnhancementFails_AvatarAddress",
                table: "ItemEnhancementFails",
                column: "AvatarAddress");

            migrationBuilder.CreateIndex(
                name: "IX_JoinArenas_AgentAddress",
                table: "JoinArenas",
                column: "AgentAddress");

            migrationBuilder.CreateIndex(
                name: "IX_JoinArenas_AvatarAddress",
                table: "JoinArenas",
                column: "AvatarAddress");

            migrationBuilder.CreateIndex(
                name: "IX_MigrateMonsterCollections_AgentAddress1",
                table: "MigrateMonsterCollections",
                column: "AgentAddress1");

            migrationBuilder.CreateIndex(
                name: "IX_ReplaceCombinationEquipmentMaterials_AgentAddress",
                table: "ReplaceCombinationEquipmentMaterials",
                column: "AgentAddress");

            migrationBuilder.CreateIndex(
                name: "IX_ReplaceCombinationEquipmentMaterials_AvatarAddress",
                table: "ReplaceCombinationEquipmentMaterials",
                column: "AvatarAddress");

            migrationBuilder.CreateIndex(
                name: "IX_Stakings_AgentAddress",
                table: "Stakings",
                column: "AgentAddress");

            migrationBuilder.CreateIndex(
                name: "IX_UnlockEquipmentRecipes_AgentAddress",
                table: "UnlockEquipmentRecipes",
                column: "AgentAddress");

            migrationBuilder.CreateIndex(
                name: "IX_UnlockEquipmentRecipes_AvatarAddress",
                table: "UnlockEquipmentRecipes",
                column: "AvatarAddress");

            migrationBuilder.CreateIndex(
                name: "IX_UnlockWorlds_AgentAddress",
                table: "UnlockWorlds",
                column: "AgentAddress");

            migrationBuilder.CreateIndex(
                name: "IX_UnlockWorlds_AvatarAddress",
                table: "UnlockWorlds",
                column: "AvatarAddress");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BattleArenaRanking");

            migrationBuilder.DropTable(
                name: "BattleArenas");

            migrationBuilder.DropTable(
                name: "ClaimStakeRewards");

            migrationBuilder.DropTable(
                name: "Grindings");

            migrationBuilder.DropTable(
                name: "HasRandomBuffs");

            migrationBuilder.DropTable(
                name: "HasWithRandomBuffs");

            migrationBuilder.DropTable(
                name: "ItemEnhancementFails");

            migrationBuilder.DropTable(
                name: "JoinArenas");

            migrationBuilder.DropTable(
                name: "MigrateMonsterCollections");

            migrationBuilder.DropTable(
                name: "ReplaceCombinationEquipmentMaterials");

            migrationBuilder.DropTable(
                name: "ShopConsumables");

            migrationBuilder.DropTable(
                name: "ShopCostumes");

            migrationBuilder.DropTable(
                name: "ShopEquipments");

            migrationBuilder.DropTable(
                name: "ShopMaterials");

            migrationBuilder.DropTable(
                name: "Stakings");

            migrationBuilder.DropTable(
                name: "UnlockEquipmentRecipes");

            migrationBuilder.DropTable(
                name: "UnlockWorlds");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StageRanking",
                table: "StageRanking");

            migrationBuilder.AlterColumn<int>(
                name: "Ranking",
                table: "StageRanking",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<string>(
                name: "AvatarAddress",
                table: "StageRanking",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "TimeStamp",
                table: "ShopHistoryMaterials",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "TimeStamp",
                table: "ShopHistoryEquipments",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "TimeStamp",
                table: "ShopHistoryCostumes",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "TimeStamp",
                table: "ShopHistoryConsumables",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StageRanking",
                table: "StageRanking",
                column: "Ranking");
        }
    }
}
