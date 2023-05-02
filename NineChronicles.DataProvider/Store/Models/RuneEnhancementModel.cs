namespace NineChronicles.DataProvider.Store.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public class RuneEnhancementModel
    {
        [Key]
        public string? Id { get; set; }

        public long BlockIndex { get; set; }

        public string? AgentAddress { get; set; }

        public AgentModel? Agent { get; set; }

        public string? AvatarAddress { get; set; }

        public AvatarModel? Avatar { get; set; }

        public int PreviousRuneLevel { get; set; }

        public int OutputRuneLevel { get; set; }

        public int RuneId { get; set; }

        public int TryCount { get; set; }

        public decimal BurntNCG { get; set; }

        public decimal BurntCrystal { get; set; }

        public decimal BurntRune { get; set; }

        public DateTimeOffset Date { get; set; }

        public DateTimeOffset TimeStamp { get; set; }
    }
}
