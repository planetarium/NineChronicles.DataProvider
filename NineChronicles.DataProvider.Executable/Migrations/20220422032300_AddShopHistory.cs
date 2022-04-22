using Microsoft.EntityFrameworkCore.Migrations;

namespace NineChronicles.DataProvider.Executable.Migrations
{
    public partial class AddShopHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_StageRankings",
                table: "StageRankings");

            migrationBuilder.RenameTable(
                name: "StageRankings",
                newName: "StageRanking");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StageRanking",
                table: "StageRanking",
                column: "Ranking");

            migrationBuilder.CreateTable(
                name: "ShopHistoryConsumables",
                columns: table => new
                {
                    OrderId = table.Column<string>(type: "TEXT", nullable: false),
                    TxId = table.Column<string>(type: "TEXT", nullable: true),
                    BlockIndex = table.Column<long>(type: "INTEGER", nullable: false),
                    BlockHash = table.Column<string>(type: "TEXT", nullable: true),
                    ItemId = table.Column<string>(type: "TEXT", nullable: true),
                    SellerAvatarAddress = table.Column<string>(type: "TEXT", nullable: true),
                    BuyerAvatarAddress = table.Column<string>(type: "TEXT", nullable: true),
                    Price = table.Column<decimal>(type: "TEXT", nullable: false),
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
                    ItemCount = table.Column<int>(type: "INTEGER", nullable: false),
                    TimeStamp = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopHistoryConsumables", x => x.OrderId);
                });

            migrationBuilder.CreateTable(
                name: "ShopHistoryCostumes",
                columns: table => new
                {
                    OrderId = table.Column<string>(type: "TEXT", nullable: false),
                    TxId = table.Column<string>(type: "TEXT", nullable: true),
                    BlockIndex = table.Column<long>(type: "INTEGER", nullable: false),
                    BlockHash = table.Column<string>(type: "TEXT", nullable: true),
                    ItemId = table.Column<string>(type: "TEXT", nullable: true),
                    SellerAvatarAddress = table.Column<string>(type: "TEXT", nullable: true),
                    BuyerAvatarAddress = table.Column<string>(type: "TEXT", nullable: true),
                    Price = table.Column<decimal>(type: "TEXT", nullable: false),
                    ItemType = table.Column<string>(type: "TEXT", nullable: true),
                    ItemSubType = table.Column<string>(type: "TEXT", nullable: true),
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    ElementalType = table.Column<string>(type: "TEXT", nullable: true),
                    Grade = table.Column<int>(type: "INTEGER", nullable: false),
                    SetId = table.Column<int>(type: "INTEGER", nullable: false),
                    Equipped = table.Column<bool>(type: "INTEGER", nullable: false),
                    SpineResourcePath = table.Column<string>(type: "TEXT", nullable: true),
                    RequiredBlockIndex = table.Column<long>(type: "INTEGER", nullable: false),
                    NonFungibleId = table.Column<string>(type: "TEXT", nullable: true),
                    TradableId = table.Column<string>(type: "TEXT", nullable: true),
                    ItemCount = table.Column<int>(type: "INTEGER", nullable: false),
                    TimeStamp = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopHistoryCostumes", x => x.OrderId);
                });

            migrationBuilder.CreateTable(
                name: "ShopHistoryEquipments",
                columns: table => new
                {
                    OrderId = table.Column<string>(type: "TEXT", nullable: false),
                    TxId = table.Column<string>(type: "TEXT", nullable: true),
                    BlockIndex = table.Column<long>(type: "INTEGER", nullable: false),
                    BlockHash = table.Column<string>(type: "TEXT", nullable: true),
                    ItemId = table.Column<string>(type: "TEXT", nullable: true),
                    SellerAvatarAddress = table.Column<string>(type: "TEXT", nullable: true),
                    BuyerAvatarAddress = table.Column<string>(type: "TEXT", nullable: true),
                    Price = table.Column<decimal>(type: "TEXT", nullable: false),
                    ItemType = table.Column<string>(type: "TEXT", nullable: true),
                    ItemSubType = table.Column<string>(type: "TEXT", nullable: true),
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    BuffSkillCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ElementalType = table.Column<string>(type: "TEXT", nullable: true),
                    Grade = table.Column<int>(type: "INTEGER", nullable: false),
                    SetId = table.Column<int>(type: "INTEGER", nullable: false),
                    SkillsCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SpineResourcePath = table.Column<string>(type: "TEXT", nullable: true),
                    RequiredBlockIndex = table.Column<long>(type: "INTEGER", nullable: false),
                    NonFungibleId = table.Column<string>(type: "TEXT", nullable: true),
                    TradableId = table.Column<string>(type: "TEXT", nullable: true),
                    UniqueStatType = table.Column<string>(type: "TEXT", nullable: true),
                    ItemCount = table.Column<int>(type: "INTEGER", nullable: false),
                    TimeStamp = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopHistoryEquipments", x => x.OrderId);
                });

            migrationBuilder.CreateTable(
                name: "ShopHistoryMaterials",
                columns: table => new
                {
                    OrderId = table.Column<string>(type: "TEXT", nullable: false),
                    TxId = table.Column<string>(type: "TEXT", nullable: true),
                    BlockIndex = table.Column<long>(type: "INTEGER", nullable: false),
                    BlockHash = table.Column<string>(type: "TEXT", nullable: true),
                    ItemId = table.Column<string>(type: "TEXT", nullable: true),
                    SellerAvatarAddress = table.Column<string>(type: "TEXT", nullable: true),
                    BuyerAvatarAddress = table.Column<string>(type: "TEXT", nullable: true),
                    Price = table.Column<decimal>(type: "TEXT", nullable: false),
                    ItemType = table.Column<string>(type: "TEXT", nullable: true),
                    ItemSubType = table.Column<string>(type: "TEXT", nullable: true),
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    ElementalType = table.Column<string>(type: "TEXT", nullable: true),
                    Grade = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemCount = table.Column<int>(type: "INTEGER", nullable: false),
                    TimeStamp = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopHistoryMaterials", x => x.OrderId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShopHistoryConsumables");

            migrationBuilder.DropTable(
                name: "ShopHistoryCostumes");

            migrationBuilder.DropTable(
                name: "ShopHistoryEquipments");

            migrationBuilder.DropTable(
                name: "ShopHistoryMaterials");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StageRanking",
                table: "StageRanking");

            migrationBuilder.RenameTable(
                name: "StageRanking",
                newName: "StageRankings");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StageRankings",
                table: "StageRankings",
                column: "Ranking");
        }
    }
}
