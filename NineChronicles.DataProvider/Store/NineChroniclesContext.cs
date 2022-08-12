namespace NineChronicles.DataProvider.Store
{
    using Microsoft.EntityFrameworkCore;
    using NineChronicles.DataProvider.Store.Models;

    public sealed class NineChroniclesContext : DbContext
    {
        public NineChroniclesContext(DbContextOptions<NineChroniclesContext> options)
            : base(options)
        {
        }

        public DbSet<HackAndSlashModel>? HackAndSlashes { get; set; }

        public DbSet<StageRankingModel>? StageRanking { get; set; }

        public DbSet<CombinationConsumableModel>? CombinationConsumables { get; set; }

        public DbSet<CombinationEquipmentModel>? CombinationEquipments { get; set; }

        public DbSet<ItemEnhancementModel>? ItemEnhancements { get; set; }

        public DbSet<CraftRankingInputModel>? CraftRankings { get; set; }

        public DbSet<CraftRankingOutputModel>? CraftRankingsOutput { get; set; }

        public DbSet<AvatarModel>? Avatars { get; set; }

        public DbSet<AgentModel>? Agents { get; set; }

        public DbSet<EquipmentModel>? Equipments { get; set; }

        public DbSet<EquipmentRankingModel>? EquipmentRanking { get; set; }

        public DbSet<AbilityRankingModel>? AbilityRanking { get; set; }

        public DbSet<ShopHistoryEquipmentModel>? ShopHistoryEquipments { get; set; }

        public DbSet<ShopHistoryCostumeModel>? ShopHistoryCostumes { get; set; }

        public DbSet<ShopHistoryMaterialModel>? ShopHistoryMaterials { get; set; }

        public DbSet<ShopHistoryConsumableModel>? ShopHistoryConsumables { get; set; }

        public DbSet<StakeModel>? Stakings { get; set; }

        public DbSet<ClaimStakeRewardModel>? ClaimStakeRewards { get; set; }

        public DbSet<MigrateMonsterCollectionModel>? MigrateMonsterCollections { get; set; }

        public DbSet<GrindingModel>? Grindings { get; set; }

        public DbSet<ItemEnhancementFailModel>? ItemEnhancementFails { get; set; }

        public DbSet<UnlockEquipmentRecipeModel>? UnlockEquipmentRecipes { get; set; }

        public DbSet<UnlockWorldModel>? UnlockWorlds { get; set; }

        public DbSet<ReplaceCombinationEquipmentMaterialModel>? ReplaceCombinationEquipmentMaterials { get; set; }

        public DbSet<HasRandomBuffModel>? HasRandomBuffs { get; set; }

        public DbSet<HasWithRandomBuffModel>? HasWithRandomBuffs { get; set; }

        public DbSet<JoinArenaModel>? JoinArenas { get; set; }

        public DbSet<BattleArenaModel>? BattleArenas { get; set; }

        public DbSet<ShopEquipmentModel>? ShopEquipments { get; set; }

        public DbSet<ShopConsumableModel>? ShopConsumables { get; set; }

        public DbSet<ShopCostumeModel>? ShopCostumes { get; set; }

        public DbSet<ShopMaterialModel>? ShopMaterials { get; set; }

        public DbSet<BattleArenaRankingModel>? BattleArenaRanking { get; set; }

        public DbSet<BlockModel>? Blocks { get; set; }

        public DbSet<TransactionModel>? Transactions { get; set; }

        public DbSet<HackAndSlashSweepModel>? HackAndSlashSweeps { get; set; }

        public DbSet<EventDungeonBattleModel>? EventDungeonBattles { get; set; }

        /*
         * This override method enables EF database update & migration when certain models are required for data querying,
         * but tables constructed by these models are not needed.
         */
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StageRankingModel>().HasNoKey();
            modelBuilder.Entity<CraftRankingOutputModel>().HasNoKey();
            modelBuilder.Entity<EquipmentRankingModel>().HasNoKey();
            modelBuilder.Entity<AbilityRankingModel>().HasNoKey();
            modelBuilder.Entity<BattleArenaRankingModel>().HasNoKey();
        }
    }
}
