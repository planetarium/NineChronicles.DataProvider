using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NineChronicles.DataProvider.Executable.Migrations
{
    public partial class AddDateIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UnlockWorlds_Date",
                table: "UnlockWorlds",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_UnlockRuneSlots_Date",
                table: "UnlockRuneSlots",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_UnlockEquipmentRecipes_Date",
                table: "UnlockEquipmentRecipes",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_TransferAssets_Date",
                table: "TransferAssets",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_Date",
                table: "Transactions",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_Stakings_Date",
                table: "Stakings",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_RunesAcquired_Date",
                table: "RunesAcquired",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_RuneEnhancements_Date",
                table: "RuneEnhancements",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_RequestPledges_Date",
                table: "RequestPledges",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_ReplaceCombinationEquipmentMaterials_Date",
                table: "ReplaceCombinationEquipmentMaterials",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_RapidCombinations_Date",
                table: "RapidCombinations",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_PetEnhancements_Date",
                table: "PetEnhancements",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_MigrateMonsterCollections_Date",
                table: "MigrateMonsterCollections",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_JoinArenas_Date",
                table: "JoinArenas",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_ItemEnhancements_Date",
                table: "ItemEnhancements",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_HasWithRandomBuffs_Date",
                table: "HasWithRandomBuffs",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_HasRandomBuffs_Date",
                table: "HasRandomBuffs",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_HackAndSlashSweeps_Date",
                table: "HackAndSlashSweeps",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_HackAndSlashes_Date",
                table: "HackAndSlashes",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_Grindings_Date",
                table: "Grindings",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_EventMaterialItemCrafts_Date",
                table: "EventMaterialItemCrafts",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_EventDungeonBattles_Date",
                table: "EventDungeonBattles",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_EventConsumableItemCrafts_Date",
                table: "EventConsumableItemCrafts",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_CombinationEquipments_Date",
                table: "CombinationEquipments",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_CombinationConsumables_Date",
                table: "CombinationConsumables",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimStakeRewards_Date",
                table: "ClaimStakeRewards",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_Blocks_Date",
                table: "Blocks",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_BattleGrandFinales_Date",
                table: "BattleGrandFinales",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_BattleArenas_Date",
                table: "BattleArenas",
                column: "Date");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UnlockWorlds_Date",
                table: "UnlockWorlds");

            migrationBuilder.DropIndex(
                name: "IX_UnlockRuneSlots_Date",
                table: "UnlockRuneSlots");

            migrationBuilder.DropIndex(
                name: "IX_UnlockEquipmentRecipes_Date",
                table: "UnlockEquipmentRecipes");

            migrationBuilder.DropIndex(
                name: "IX_TransferAssets_Date",
                table: "TransferAssets");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_Date",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Stakings_Date",
                table: "Stakings");

            migrationBuilder.DropIndex(
                name: "IX_RunesAcquired_Date",
                table: "RunesAcquired");

            migrationBuilder.DropIndex(
                name: "IX_RuneEnhancements_Date",
                table: "RuneEnhancements");

            migrationBuilder.DropIndex(
                name: "IX_RequestPledges_Date",
                table: "RequestPledges");

            migrationBuilder.DropIndex(
                name: "IX_ReplaceCombinationEquipmentMaterials_Date",
                table: "ReplaceCombinationEquipmentMaterials");

            migrationBuilder.DropIndex(
                name: "IX_RapidCombinations_Date",
                table: "RapidCombinations");

            migrationBuilder.DropIndex(
                name: "IX_PetEnhancements_Date",
                table: "PetEnhancements");

            migrationBuilder.DropIndex(
                name: "IX_MigrateMonsterCollections_Date",
                table: "MigrateMonsterCollections");

            migrationBuilder.DropIndex(
                name: "IX_JoinArenas_Date",
                table: "JoinArenas");

            migrationBuilder.DropIndex(
                name: "IX_ItemEnhancements_Date",
                table: "ItemEnhancements");

            migrationBuilder.DropIndex(
                name: "IX_HasWithRandomBuffs_Date",
                table: "HasWithRandomBuffs");

            migrationBuilder.DropIndex(
                name: "IX_HasRandomBuffs_Date",
                table: "HasRandomBuffs");

            migrationBuilder.DropIndex(
                name: "IX_HackAndSlashSweeps_Date",
                table: "HackAndSlashSweeps");

            migrationBuilder.DropIndex(
                name: "IX_HackAndSlashes_Date",
                table: "HackAndSlashes");

            migrationBuilder.DropIndex(
                name: "IX_Grindings_Date",
                table: "Grindings");

            migrationBuilder.DropIndex(
                name: "IX_EventMaterialItemCrafts_Date",
                table: "EventMaterialItemCrafts");

            migrationBuilder.DropIndex(
                name: "IX_EventDungeonBattles_Date",
                table: "EventDungeonBattles");

            migrationBuilder.DropIndex(
                name: "IX_EventConsumableItemCrafts_Date",
                table: "EventConsumableItemCrafts");

            migrationBuilder.DropIndex(
                name: "IX_CombinationEquipments_Date",
                table: "CombinationEquipments");

            migrationBuilder.DropIndex(
                name: "IX_CombinationConsumables_Date",
                table: "CombinationConsumables");

            migrationBuilder.DropIndex(
                name: "IX_ClaimStakeRewards_Date",
                table: "ClaimStakeRewards");

            migrationBuilder.DropIndex(
                name: "IX_Blocks_Date",
                table: "Blocks");

            migrationBuilder.DropIndex(
                name: "IX_BattleGrandFinales_Date",
                table: "BattleGrandFinales");

            migrationBuilder.DropIndex(
                name: "IX_BattleArenas_Date",
                table: "BattleArenas");
        }
    }
}
