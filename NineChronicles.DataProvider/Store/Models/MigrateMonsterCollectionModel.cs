namespace NineChronicles.DataProvider.Store.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public class MigrateMonsterCollectionModel
    {
        public long BlockIndex { get; set; }

        [Key]
        public string? AgentAddress { get; set; }

        public AgentModel? Agent { get; set; }

        public decimal MigrationAmount { get; set; }

        public long MigrationStartBlockIndex { get; set; }

        public long StakeStartBlockIndex { get; set; }

        public DateTimeOffset TimeStamp { get; set; }
    }
}
