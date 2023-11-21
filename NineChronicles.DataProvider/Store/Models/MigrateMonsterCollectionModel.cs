namespace NineChronicles.DataProvider.Store.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using Microsoft.EntityFrameworkCore;

    [Index(nameof(Date))]

    public class MigrateMonsterCollectionModel
    {
        public long BlockIndex { get; set; }

        [Key]
        public string? AgentAddress { get; set; }

        public AgentModel? Agent { get; set; }

        public decimal MigrationAmount { get; set; }

        public long MigrationStartBlockIndex { get; set; }

        public long StakeStartBlockIndex { get; set; }

        public DateOnly Date { get; set; }

        public DateTimeOffset TimeStamp { get; set; }
    }
}
