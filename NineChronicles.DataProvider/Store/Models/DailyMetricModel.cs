namespace NineChronicles.DataProvider.Store.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using Microsoft.EntityFrameworkCore;

    [Index(nameof(Date))]

    public class DailyMetricModel
    {
        [Key]
        public DateOnly Date { get; set; }

        public int? Dau { get; set; }

        public int? TxCount { get; set; }

        public int? DailyNew { get; set; }

        public int? HackAndSlashCount { get; set; }

        public int? HackAndSlashUsers { get; set; }

        public int? SweepCount { get; set; }

        public int? SweepUsers { get; set; }

        public int? CraftingEquipmentCount { get; set; }

        public int? CraftingEquipmentUsers { get; set; }

        public int? CraftingConsumableCount { get; set; }

        public int? CraftingConsumableUsers { get; set; }

        public int? EnhanceCount { get; set; }

        public int? EnhanceUsers { get; set; }

        public int? AuraSummonCount { get; set; }

        public int? RuneSummonCount { get; set; }

        public int? ApUsage { get; set; }

        public int? HourglassUsage { get; set; }

        public decimal? NcgTrade { get; set; }

        public decimal? EnhanceNcg { get; set; }

        public decimal? RuneNcg { get; set; }

        public decimal? RuneSlotNcg { get; set; }

        public decimal? ArenaNcg { get; set; }

        public decimal? EventTicketNcg { get; set; }
    }
}
