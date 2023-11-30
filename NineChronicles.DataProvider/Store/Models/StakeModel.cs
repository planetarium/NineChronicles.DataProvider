namespace NineChronicles.DataProvider.Store.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using Microsoft.EntityFrameworkCore;

    [Index(nameof(Date))]

    public class StakeModel
    {
        [Key]
        public string? Id { get; set; }

        public long BlockIndex { get; set; }

        public string? AgentAddress { get; set; }

        public AgentModel? Agent { get; set; }

        public decimal PreviousAmount { get; set; }

        public decimal NewAmount { get; set; }

        public decimal RemainingNCG { get; set; }

        public long PrevStakeStartBlockIndex { get; set; }

        public long NewStakeStartBlockIndex { get; set; }

        public DateOnly Date { get; set; }

        public DateTimeOffset TimeStamp { get; set; }
    }
}
