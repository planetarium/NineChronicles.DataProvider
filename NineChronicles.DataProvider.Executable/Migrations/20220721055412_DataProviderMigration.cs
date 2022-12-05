using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NineChronicles.DataProvider.Executable.Migrations
{
    public partial class DataProviderMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AbilityRanking",
                columns: table => new
                {
                    AvatarAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AgentAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarLevel = table.Column<int>(type: "int", nullable: true),
                    TitleId = table.Column<int>(type: "int", nullable: true),
                    ArmorId = table.Column<int>(type: "int", nullable: true),
                    Cp = table.Column<int>(type: "int", nullable: true),
                    Ranking = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Agents",
                columns: table => new
                {
                    Address = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agents", x => x.Address);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BattleArenaRanking",
                columns: table => new
                {
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    AgentAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarLevel = table.Column<int>(type: "int", nullable: false),
                    ChampionshipId = table.Column<int>(type: "int", nullable: false),
                    Round = table.Column<int>(type: "int", nullable: false),
                    ArenaType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Score = table.Column<int>(type: "int", nullable: false),
                    WinCount = table.Column<int>(type: "int", nullable: false),
                    MedalCount = table.Column<int>(type: "int", nullable: false),
                    LossCount = table.Column<int>(type: "int", nullable: false),
                    Ticket = table.Column<int>(type: "int", nullable: false),
                    PurchasedTicketCount = table.Column<int>(type: "int", nullable: false),
                    TicketResetCount = table.Column<int>(type: "int", nullable: false),
                    EntranceFee = table.Column<long>(type: "bigint", nullable: false),
                    TicketPrice = table.Column<long>(type: "bigint", nullable: false),
                    AdditionalTicketPrice = table.Column<long>(type: "bigint", nullable: false),
                    RequiredMedalCount = table.Column<int>(type: "int", nullable: false),
                    StartBlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    EndBlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TitleId = table.Column<int>(type: "int", nullable: true),
                    ArmorId = table.Column<int>(type: "int", nullable: true),
                    Cp = table.Column<int>(type: "int", nullable: true),
                    Ranking = table.Column<int>(type: "int", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CraftRankings",
                columns: table => new
                {
                    AvatarAddress = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AgentAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    CraftCount = table.Column<int>(type: "int", nullable: false),
                    Ranking = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CraftRankings", x => x.AvatarAddress);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CraftRankingsOutput",
                columns: table => new
                {
                    AvatarAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AgentAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    CraftCount = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarLevel = table.Column<int>(type: "int", nullable: true),
                    TitleId = table.Column<int>(type: "int", nullable: true),
                    ArmorId = table.Column<int>(type: "int", nullable: true),
                    Cp = table.Column<int>(type: "int", nullable: true),
                    Ranking = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EquipmentRanking",
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
                name: "Equipments",
                columns: table => new
                {
                    ItemId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AgentAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EquipmentId = table.Column<int>(type: "int", nullable: false),
                    Cp = table.Column<int>(type: "int", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    ItemSubType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipments", x => x.ItemId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ShopConsumables",
                columns: table => new
                {
                    ItemId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    SellerAgentAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SellerAvatarAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ItemType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ItemSubType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Id = table.Column<int>(type: "int", nullable: false),
                    BuffSkillCount = table.Column<int>(type: "int", nullable: false),
                    ElementalType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Grade = table.Column<int>(type: "int", nullable: false),
                    SkillsCount = table.Column<int>(type: "int", nullable: false),
                    RequiredBlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    NonFungibleId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TradableId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MainStat = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Price = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    OrderId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CombatPoint = table.Column<int>(type: "int", nullable: false),
                    ItemCount = table.Column<int>(type: "int", nullable: false),
                    SellStartedBlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    SellExpiredBlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopConsumables", x => x.ItemId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ShopCostumes",
                columns: table => new
                {
                    ItemId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    SellerAgentAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SellerAvatarAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ItemType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ItemSubType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Id = table.Column<int>(type: "int", nullable: false),
                    ElementalType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Grade = table.Column<int>(type: "int", nullable: false),
                    Equipped = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SpineResourcePath = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequiredBlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    NonFungibleId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TradableId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Price = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    OrderId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CombatPoint = table.Column<int>(type: "int", nullable: false),
                    ItemCount = table.Column<int>(type: "int", nullable: false),
                    SellStartedBlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    SellExpiredBlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopCostumes", x => x.ItemId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ShopEquipments",
                columns: table => new
                {
                    ItemId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    SellerAgentAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SellerAvatarAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ItemType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ItemSubType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Id = table.Column<int>(type: "int", nullable: false),
                    BuffSkillCount = table.Column<int>(type: "int", nullable: false),
                    ElementalType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Grade = table.Column<int>(type: "int", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    SetId = table.Column<int>(type: "int", nullable: false),
                    SkillsCount = table.Column<int>(type: "int", nullable: false),
                    SpineResourcePath = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequiredBlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    NonFungibleId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TradableId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UniqueStatType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Price = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    OrderId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CombatPoint = table.Column<int>(type: "int", nullable: false),
                    ItemCount = table.Column<int>(type: "int", nullable: false),
                    SellStartedBlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    SellExpiredBlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopEquipments", x => x.ItemId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ShopHistoryConsumables",
                columns: table => new
                {
                    OrderId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TxId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    BlockHash = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ItemId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SellerAvatarAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BuyerAvatarAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Price = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    ItemType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ItemSubType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Id = table.Column<int>(type: "int", nullable: false),
                    BuffSkillCount = table.Column<int>(type: "int", nullable: false),
                    ElementalType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Grade = table.Column<int>(type: "int", nullable: false),
                    SkillsCount = table.Column<int>(type: "int", nullable: false),
                    RequiredBlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    NonFungibleId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TradableId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MainStat = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ItemCount = table.Column<int>(type: "int", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopHistoryConsumables", x => x.OrderId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ShopHistoryCostumes",
                columns: table => new
                {
                    OrderId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TxId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    BlockHash = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ItemId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SellerAvatarAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BuyerAvatarAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Price = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    ItemType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ItemSubType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Id = table.Column<int>(type: "int", nullable: false),
                    ElementalType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Grade = table.Column<int>(type: "int", nullable: false),
                    SetId = table.Column<int>(type: "int", nullable: false),
                    Equipped = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SpineResourcePath = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequiredBlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    NonFungibleId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TradableId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ItemCount = table.Column<int>(type: "int", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopHistoryCostumes", x => x.OrderId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ShopHistoryEquipments",
                columns: table => new
                {
                    OrderId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TxId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    BlockHash = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ItemId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SellerAvatarAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BuyerAvatarAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Price = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    ItemType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ItemSubType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Id = table.Column<int>(type: "int", nullable: false),
                    BuffSkillCount = table.Column<int>(type: "int", nullable: false),
                    ElementalType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Grade = table.Column<int>(type: "int", nullable: false),
                    SetId = table.Column<int>(type: "int", nullable: false),
                    SkillsCount = table.Column<int>(type: "int", nullable: false),
                    SpineResourcePath = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequiredBlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    NonFungibleId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TradableId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UniqueStatType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ItemCount = table.Column<int>(type: "int", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopHistoryEquipments", x => x.OrderId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ShopHistoryMaterials",
                columns: table => new
                {
                    OrderId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TxId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    BlockHash = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ItemId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SellerAvatarAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BuyerAvatarAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Price = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    ItemType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ItemSubType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Id = table.Column<int>(type: "int", nullable: false),
                    ElementalType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Grade = table.Column<int>(type: "int", nullable: false),
                    ItemCount = table.Column<int>(type: "int", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopHistoryMaterials", x => x.OrderId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ShopMaterials",
                columns: table => new
                {
                    ItemId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    SellerAgentAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SellerAvatarAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ItemType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ItemSubType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Id = table.Column<int>(type: "int", nullable: false),
                    ElementalType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Grade = table.Column<int>(type: "int", nullable: false),
                    TradableId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Price = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    OrderId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CombatPoint = table.Column<int>(type: "int", nullable: false),
                    ItemCount = table.Column<int>(type: "int", nullable: false),
                    SellStartedBlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    SellExpiredBlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopMaterials", x => x.ItemId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "StageRanking",
                columns: table => new
                {
                    Ranking = table.Column<int>(type: "int", nullable: false),
                    ClearedStageId = table.Column<int>(type: "int", nullable: false),
                    AvatarAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AgentAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarLevel = table.Column<int>(type: "int", nullable: true),
                    TitleId = table.Column<int>(type: "int", nullable: true),
                    ArmorId = table.Column<int>(type: "int", nullable: true),
                    Cp = table.Column<int>(type: "int", nullable: true),
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Avatars",
                columns: table => new
                {
                    Address = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AgentAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarLevel = table.Column<int>(type: "int", nullable: true),
                    TitleId = table.Column<int>(type: "int", nullable: true),
                    ArmorId = table.Column<int>(type: "int", nullable: true),
                    Cp = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Avatars", x => x.Address);
                    table.ForeignKey(
                        name: "FK_Avatars_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ClaimStakeRewards",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    AgentAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClaimRewardAvatarAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HourGlassCount = table.Column<int>(type: "int", nullable: false),
                    ApPotionCount = table.Column<int>(type: "int", nullable: false),
                    ClaimStakeStartBlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    ClaimStakeEndBlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimStakeRewards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimStakeRewards_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MigrateMonsterCollections",
                columns: table => new
                {
                    AgentAddress = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    AgentAddress1 = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MigrationAmount = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    MigrationStartBlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    StakeStartBlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Stakings",
                columns: table => new
                {
                    TimeStamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    AgentAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PreviousAmount = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    NewAmount = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    RemainingNCG = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    PrevStakeStartBlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    NewStakeStartBlockIndex = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stakings", x => x.TimeStamp);
                    table.ForeignKey(
                        name: "FK_Stakings_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BattleArenas",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    AgentAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarLevel = table.Column<int>(type: "int", nullable: false),
                    EnemyAvatarAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChampionshipId = table.Column<int>(type: "int", nullable: false),
                    Round = table.Column<int>(type: "int", nullable: false),
                    TicketCount = table.Column<int>(type: "int", nullable: false),
                    BurntNCG = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Victory = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MedalCount = table.Column<int>(type: "int", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BattleArenas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BattleArenas_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address");
                    table.ForeignKey(
                        name: "FK_BattleArenas_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CombinationConsumables",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AgentAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RecipeId = table.Column<int>(type: "int", nullable: false),
                    SlotIndex = table.Column<int>(type: "int", nullable: false),
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CombinationConsumables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CombinationConsumables_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address");
                    table.ForeignKey(
                        name: "FK_CombinationConsumables_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CombinationEquipments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AgentAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RecipeId = table.Column<int>(type: "int", nullable: false),
                    SlotIndex = table.Column<int>(type: "int", nullable: false),
                    SubRecipeId = table.Column<int>(type: "int", nullable: true),
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CombinationEquipments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CombinationEquipments_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address");
                    table.ForeignKey(
                        name: "FK_CombinationEquipments_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Grindings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    AgentAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EquipmentItemId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EquipmentId = table.Column<int>(type: "int", nullable: false),
                    EquipmentLevel = table.Column<int>(type: "int", nullable: false),
                    Crystal = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Grindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Grindings_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address");
                    table.ForeignKey(
                        name: "FK_Grindings_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "HackAndSlashes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AgentAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StageId = table.Column<int>(type: "int", nullable: false),
                    Cleared = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Mimisbrunnr = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HackAndSlashes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HackAndSlashes_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address");
                    table.ForeignKey(
                        name: "FK_HackAndSlashes_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "HasRandomBuffs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    AgentAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HasStageId = table.Column<int>(type: "int", nullable: false),
                    GachaCount = table.Column<int>(type: "int", nullable: false),
                    BurntCrystal = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HasRandomBuffs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HasRandomBuffs_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address");
                    table.ForeignKey(
                        name: "FK_HasRandomBuffs_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "HasWithRandomBuffs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    AgentAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StageId = table.Column<int>(type: "int", nullable: false),
                    BuffId = table.Column<int>(type: "int", nullable: false),
                    Cleared = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HasWithRandomBuffs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HasWithRandomBuffs_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address");
                    table.ForeignKey(
                        name: "FK_HasWithRandomBuffs_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ItemEnhancementFails",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    AgentAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EquipmentItemId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaterialItemId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EquipmentLevel = table.Column<int>(type: "int", nullable: false),
                    GainedCrystal = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    BurntNCG = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemEnhancementFails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemEnhancementFails_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address");
                    table.ForeignKey(
                        name: "FK_ItemEnhancementFails_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ItemEnhancements",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AgentAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ItemId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaterialId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SlotIndex = table.Column<int>(type: "int", nullable: false),
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemEnhancements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemEnhancements_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address");
                    table.ForeignKey(
                        name: "FK_ItemEnhancements_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "JoinArenas",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    AgentAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarLevel = table.Column<int>(type: "int", nullable: false),
                    ArenaRound = table.Column<int>(type: "int", nullable: false),
                    ChampionshipId = table.Column<int>(type: "int", nullable: false),
                    BurntCrystal = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JoinArenas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JoinArenas_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address");
                    table.ForeignKey(
                        name: "FK_JoinArenas_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ReplaceCombinationEquipmentMaterials",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    AgentAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReplacedMaterialId = table.Column<int>(type: "int", nullable: false),
                    ReplacedMaterialCount = table.Column<int>(type: "int", nullable: false),
                    BurntCrystal = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplaceCombinationEquipmentMaterials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReplaceCombinationEquipmentMaterials_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address");
                    table.ForeignKey(
                        name: "FK_ReplaceCombinationEquipmentMaterials_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UnlockEquipmentRecipes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    AgentAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UnlockEquipmentRecipeId = table.Column<int>(type: "int", nullable: false),
                    BurntCrystal = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnlockEquipmentRecipes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnlockEquipmentRecipes_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address");
                    table.ForeignKey(
                        name: "FK_UnlockEquipmentRecipes_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UnlockWorlds",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    AgentAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UnlockWorldId = table.Column<int>(type: "int", nullable: false),
                    BurntCrystal = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnlockWorlds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnlockWorlds_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address");
                    table.ForeignKey(
                        name: "FK_UnlockWorlds_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Avatars_AgentAddress",
                table: "Avatars",
                column: "AgentAddress");

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
                name: "IX_CombinationConsumables_AgentAddress",
                table: "CombinationConsumables",
                column: "AgentAddress");

            migrationBuilder.CreateIndex(
                name: "IX_CombinationConsumables_AvatarAddress",
                table: "CombinationConsumables",
                column: "AvatarAddress");

            migrationBuilder.CreateIndex(
                name: "IX_CombinationEquipments_AgentAddress",
                table: "CombinationEquipments",
                column: "AgentAddress");

            migrationBuilder.CreateIndex(
                name: "IX_CombinationEquipments_AvatarAddress",
                table: "CombinationEquipments",
                column: "AvatarAddress");

            migrationBuilder.CreateIndex(
                name: "IX_Grindings_AgentAddress",
                table: "Grindings",
                column: "AgentAddress");

            migrationBuilder.CreateIndex(
                name: "IX_Grindings_AvatarAddress",
                table: "Grindings",
                column: "AvatarAddress");

            migrationBuilder.CreateIndex(
                name: "IX_HackAndSlashes_AgentAddress",
                table: "HackAndSlashes",
                column: "AgentAddress");

            migrationBuilder.CreateIndex(
                name: "IX_HackAndSlashes_AvatarAddress",
                table: "HackAndSlashes",
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
                name: "IX_ItemEnhancements_AgentAddress",
                table: "ItemEnhancements",
                column: "AgentAddress");

            migrationBuilder.CreateIndex(
                name: "IX_ItemEnhancements_AvatarAddress",
                table: "ItemEnhancements",
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
                name: "AbilityRanking");

            migrationBuilder.DropTable(
                name: "BattleArenaRanking");

            migrationBuilder.DropTable(
                name: "BattleArenas");

            migrationBuilder.DropTable(
                name: "ClaimStakeRewards");

            migrationBuilder.DropTable(
                name: "CombinationConsumables");

            migrationBuilder.DropTable(
                name: "CombinationEquipments");

            migrationBuilder.DropTable(
                name: "CraftRankings");

            migrationBuilder.DropTable(
                name: "CraftRankingsOutput");

            migrationBuilder.DropTable(
                name: "EquipmentRanking");

            migrationBuilder.DropTable(
                name: "Equipments");

            migrationBuilder.DropTable(
                name: "Grindings");

            migrationBuilder.DropTable(
                name: "HackAndSlashes");

            migrationBuilder.DropTable(
                name: "HasRandomBuffs");

            migrationBuilder.DropTable(
                name: "HasWithRandomBuffs");

            migrationBuilder.DropTable(
                name: "ItemEnhancementFails");

            migrationBuilder.DropTable(
                name: "ItemEnhancements");

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
                name: "ShopHistoryConsumables");

            migrationBuilder.DropTable(
                name: "ShopHistoryCostumes");

            migrationBuilder.DropTable(
                name: "ShopHistoryEquipments");

            migrationBuilder.DropTable(
                name: "ShopHistoryMaterials");

            migrationBuilder.DropTable(
                name: "ShopMaterials");

            migrationBuilder.DropTable(
                name: "StageRanking");

            migrationBuilder.DropTable(
                name: "Stakings");

            migrationBuilder.DropTable(
                name: "UnlockEquipmentRecipes");

            migrationBuilder.DropTable(
                name: "UnlockWorlds");

            migrationBuilder.DropTable(
                name: "Avatars");

            migrationBuilder.DropTable(
                name: "Agents");
        }
    }
}
