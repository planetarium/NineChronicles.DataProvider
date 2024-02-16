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

        // Table for storing HackAndSlash actions
        public DbSet<HackAndSlashModel>? HackAndSlashes { get; set; }

        // Table for ranking avatars' world stage rankings
        public DbSet<StageRankingModel>? StageRanking { get; set; }

        // Table for storing CombinationConsumable actions
        public DbSet<CombinationConsumableModel>? CombinationConsumables { get; set; }

        // Table for storing CombinationEquipment actions
        public DbSet<CombinationEquipmentModel>? CombinationEquipments { get; set; }

        // Table for storing ItemEnhancement actions
        public DbSet<ItemEnhancementModel>? ItemEnhancements { get; set; }

        // Table for ranking avatars' total craft counts
        public DbSet<CraftRankingModel>? CraftRankings { get; set; }

        // Table for storing avatar information
        public DbSet<AvatarModel>? Avatars { get; set; }

        // Table for storing agent information
        public DbSet<AgentModel>? Agents { get; set; }

        // Table for storing all equipments in the network
        public DbSet<EquipmentModel>? Equipments { get; set; }

        // Table for ranking avatars' equipment combat points
        public DbSet<EquipmentRankingModel>? EquipmentRanking { get; set; }

        // Table for ranking avatars' armor combat points
        public DbSet<EquipmentRankingArmorModel>? EquipmentRankingArmor { get; set; }

        // Table for ranking avatars' belt combat points
        public DbSet<EquipmentRankingBeltModel>? EquipmentRankingBelt { get; set; }

        // Table for ranking avatars' necklace combat points
        public DbSet<EquipmentRankingNecklaceModel>? EquipmentRankingNecklace { get; set; }

        // Table for ranking avatars' ring combat points
        public DbSet<EquipmentRankingRingModel>? EquipmentRankingRing { get; set; }

        // Table for ranking avatars' weapon combat points
        public DbSet<EquipmentRankingWeaponModel>? EquipmentRankingWeapon { get; set; }

        // Table for ranking avatars' total combat points
        public DbSet<AbilityRankingModel>? AbilityRanking { get; set; }

        // Table for storing equipment purchase data in the shop
        public DbSet<ShopHistoryEquipmentModel>? ShopHistoryEquipments { get; set; }

        // Table for storing costume purchase data in the shop
        public DbSet<ShopHistoryCostumeModel>? ShopHistoryCostumes { get; set; }

        // Table for storing material purchase data in the shop
        public DbSet<ShopHistoryMaterialModel>? ShopHistoryMaterials { get; set; }

        // Table for storing consumable purchase data in the shop
        public DbSet<ShopHistoryConsumableModel>? ShopHistoryConsumables { get; set; }

        // Table for storing FungibleAssetValue purchase data in the shop
        public DbSet<ShopHistoryFungibleAssetValueModel>? ShopHistoryFungibleAssetValues { get; set; }

        // Table for storing Staking actions
        public DbSet<StakeModel>? Stakings { get; set; }

        // Table for storing ClaimStakeReward actions
        public DbSet<ClaimStakeRewardModel>? ClaimStakeRewards { get; set; }

        // Table for storing MigrateMonsterCollection actions
        public DbSet<MigrateMonsterCollectionModel>? MigrateMonsterCollections { get; set; }

        // Table for storing Grinding actions
        public DbSet<GrindingModel>? Grindings { get; set; }

        // Table for storing failed ItemEnhancement actions
        public DbSet<ItemEnhancementFailModel>? ItemEnhancementFails { get; set; }

        // Table for storing UnlockEquipmentRecipe actions
        public DbSet<UnlockEquipmentRecipeModel>? UnlockEquipmentRecipes { get; set; }

        // Table for storing UnlockWorld actions
        public DbSet<UnlockWorldModel>? UnlockWorlds { get; set; }

        // Table for storing ReplaceCombinationEquipmentMaterial actions
        public DbSet<ReplaceCombinationEquipmentMaterialModel>? ReplaceCombinationEquipmentMaterials { get; set; }

        // Table for storing HasRandomBuff actions
        public DbSet<HasRandomBuffModel>? HasRandomBuffs { get; set; }

        // Table for storing HackAndSlash actions using random buffs
        public DbSet<HasWithRandomBuffModel>? HasWithRandomBuffs { get; set; }

        // Table for storing JoinArena actions
        public DbSet<JoinArenaModel>? JoinArenas { get; set; }

        // Table for storing BattleArena actions
        public DbSet<BattleArenaModel>? BattleArenas { get; set; }

        // Table for storing a snapshot of all equipments in the shop (based on block index)
        public DbSet<ShopEquipmentModel>? ShopEquipments { get; set; }

        // Table for storing a snapshot of all consumables in the shop (based on block index)
        public DbSet<ShopConsumableModel>? ShopConsumables { get; set; }

        // Table for storing a snapshot of all costumes in the shop (based on block index)
        public DbSet<ShopCostumeModel>? ShopCostumes { get; set; }

        // Table for storing a snapshot of all materials in the shop (based on block index)
        public DbSet<ShopMaterialModel>? ShopMaterials { get; set; }

        // Table for ranking avatars' battle arena wins
        public DbSet<BattleArenaRankingModel>? BattleArenaRanking { get; set; }

        // Table for storing block information
        public DbSet<BlockModel> Blocks => Set<BlockModel>();

        // Table for storing transaction information
        public DbSet<TransactionModel>? Transactions { get; set; }

        // Table for storing HackAndSlashSweep actions
        public DbSet<HackAndSlashSweepModel>? HackAndSlashSweeps { get; set; }

        // Table for storing EventDungeonBattle actions
        public DbSet<EventDungeonBattleModel>? EventDungeonBattles { get; set; }

        // Table for storing EventConsumableItemCraft actions
        public DbSet<EventConsumableItemCraftsModel>? EventConsumableItemCrafts { get; set; }

        // Table for storing Raid actions
        public DbSet<RaiderModel> Raiders => Set<RaiderModel>();

        // Table for storing WorldBossSeasonMigration data
        public DbSet<WorldBossSeasonMigrationModel> WorldBossSeasonMigrationModels =>
            Set<WorldBossSeasonMigrationModel>();

        // Table for storing BattleGrandFinale actions
        public DbSet<BattleGrandFinaleModel> BattleGrandFinales => Set<BattleGrandFinaleModel>();

        // Table for storing EventMaterialItemCraft actions
        public DbSet<EventMaterialItemCraftsModel> EventMaterialItemCrafts => Set<EventMaterialItemCraftsModel>();

        // Table for storing RuneEnhancement actions
        public DbSet<RuneEnhancementModel> RuneEnhancements => Set<RuneEnhancementModel>();

        // Table for storing avatars' rune acquisition data
        public DbSet<RunesAcquiredModel> RunesAcquired => Set<RunesAcquiredModel>();

        // Table for storing UnlockRuneSlot actions
        public DbSet<UnlockRuneSlotModel> UnlockRuneSlots => Set<UnlockRuneSlotModel>();

        // Table for storing RapidCombination actions
        public DbSet<RapidCombinationModel> RapidCombinations => Set<RapidCombinationModel>();

        // Table for storing PetEnhancement actions
        public DbSet<PetEnhancementModel> PetEnhancements => Set<PetEnhancementModel>();

        // Table for storing TransferAsset actions
        public DbSet<TransferAssetModel> TransferAssets => Set<TransferAssetModel>();

        // Table for storing RequestPledge actions
        public DbSet<RequestPledgeModel> RequestPledges => Set<RequestPledgeModel>();

        // Table for storing AuraSummon actions
        public DbSet<AuraSummonModel> AuraSummons => Set<AuraSummonModel>();

        public DbSet<AuraSummonFailModel> AuraSummonFails => Set<AuraSummonFailModel>();

        public DbSet<UserConsumablesModel> UserConsumables => Set<UserConsumablesModel>();

        public DbSet<UserCostumesModel> UserCostumes => Set<UserCostumesModel>();

        public DbSet<UserCrystalsModel> UserCrystals => Set<UserCrystalsModel>();

        public DbSet<UserEquipmentsModel> UserEquipments => Set<UserEquipmentsModel>();

        public DbSet<UserMaterialsModel> UserMaterials => Set<UserMaterialsModel>();

        public DbSet<UserMonsterCollectionsModel> UserMonsterCollections => Set<UserMonsterCollectionsModel>();

        public DbSet<UserNCGsModel> UserNCGs => Set<UserNCGsModel>();

        public DbSet<UserRunesModel> UserRunes => Set<UserRunesModel>();

        public DbSet<UserStakingsModel> UserStakings => Set<UserStakingsModel>();

        public DbSet<RuneSummonModel> RuneSummons => Set<RuneSummonModel>();

        public DbSet<RuneSummonFailModel> RuneSummonFails => Set<RuneSummonFailModel>();

        public DbSet<ActivateCollectionModel> ActivateCollections => Set<ActivateCollectionModel>();

        /*
         * This override method enables EF database update & migration when certain models are required for data querying,
         * but tables constructed by these models are not needed.
         */
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StageRankingModel>().HasNoKey();
            modelBuilder.Entity<CraftRankingModel>().HasNoKey();
            modelBuilder.Entity<EquipmentRankingModel>().HasNoKey();
            modelBuilder.Entity<EquipmentRankingArmorModel>().HasNoKey();
            modelBuilder.Entity<EquipmentRankingBeltModel>().HasNoKey();
            modelBuilder.Entity<EquipmentRankingNecklaceModel>().HasNoKey();
            modelBuilder.Entity<EquipmentRankingRingModel>().HasNoKey();
            modelBuilder.Entity<EquipmentRankingWeaponModel>().HasNoKey();
            modelBuilder.Entity<AbilityRankingModel>().HasNoKey();
            modelBuilder.Entity<BattleArenaRankingModel>().HasNoKey();
            modelBuilder.Entity<ShopMaterialModel>().HasNoKey();
            modelBuilder.Entity<MigrateMonsterCollectionModel>().HasNoKey();
            modelBuilder.Entity<RunesAcquiredModel>().HasKey(
                nameof(RunesAcquiredModel.Id),
                nameof(RunesAcquiredModel.ActionType),
                nameof(RunesAcquiredModel.TickerType));
            modelBuilder.Entity<WorldBossRankingModel>()
                .HasNoKey()
                .ToTable("WorldBossRankings", t => t.ExcludeFromMigrations());
            modelBuilder.Entity<UserConsumablesModel>().HasNoKey();
            modelBuilder.Entity<UserCostumesModel>().HasNoKey();
            modelBuilder.Entity<UserCrystalsModel>().HasNoKey();
            modelBuilder.Entity<UserEquipmentsModel>().HasNoKey();
            modelBuilder.Entity<UserMaterialsModel>().HasNoKey();
            modelBuilder.Entity<UserMonsterCollectionsModel>().HasNoKey();
            modelBuilder.Entity<UserNCGsModel>().HasNoKey();
            modelBuilder.Entity<UserRunesModel>().HasNoKey();
            modelBuilder.Entity<UserStakingsModel>().HasNoKey();
            modelBuilder.Entity<AvatarModel>()
                .HasMany(p => p.ActivateCollections)
                .WithOne(p => p.Avatar)
                .HasForeignKey(p => p.AvatarAddress)
                .IsRequired();
            modelBuilder.Entity<ActivateCollectionModel>()
                .OwnsMany(p => p.Options, s =>
                {
                    s.WithOwner().HasForeignKey("ActivateCollectionId");
                    s.Property<int>("Id");
                    s.HasKey("Id");
                });
        }
    }
}
