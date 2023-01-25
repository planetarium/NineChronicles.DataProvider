namespace NineChronicles.DataProvider.Store.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using Microsoft.EntityFrameworkCore;

    [Index(nameof(Id), nameof(ActionType), nameof(TickerType), IsUnique = true)]
    public class RunesAcquiredModel
    {
        [Key]
        public string? Id { get; set; }

        public string? ActionType { get; set; }

        public string? TickerType { get; set; }

        public long BlockIndex { get; set; }

        public string? AgentAddress { get; set; }

        public AgentModel? Agent { get; set; }

        public string? AvatarAddress { get; set; }

        public AvatarModel? Avatar { get; set; }

        public decimal AcquiredRune { get; set; }

        public DateOnly Date { get; set; }

        public DateTimeOffset TimeStamp { get; set; }
    }
}
