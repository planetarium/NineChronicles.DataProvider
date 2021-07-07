namespace NineChronicles.DataProvider.Store
{
    using Microsoft.EntityFrameworkCore;
    using NineChronicles.DataProvider.Store.Models;

    public class NineChroniclesContext : DbContext
    {
        public NineChroniclesContext(DbContextOptions<NineChroniclesContext> options)
            : base(options)
        {
        }

        public DbSet<HackAndSlashModel>? HackAndSlashes { get; set; }

        public DbSet<StageRankingModel>? StageRankings { get; set; }

        public DbSet<CombinationConsumableModel>? CombinationConsumables { get; set; }

        public DbSet<CombinationEquipmentModel>? CombinationEquipments { get; set; }

        public DbSet<ItemEnhancementModel>? ItemEnhancements { get; set; }

        public DbSet<CraftRankingModel>? CraftRankings { get; set; }

        public DbSet<AvatarModel>? Avatars { get; set; }

        public DbSet<AgentModel>? Agents { get; set; }

        public DbSet<EquipmentModel>? Equipments { get; set; }

        public DbSet<EquipmentRankingModel>? EquipmentRankings { get; set; }
    }
}
