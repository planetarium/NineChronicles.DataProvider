namespace NineChronicles.DataProvider.Store.Models.AdventureBoss
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Numerics;

    public class AdventureBossClaimRewardModel
    {
        [Key]
        public string? Id { get; set; }

        public long BlockIndex { get; set; }

        public string? ClaimedSeason { get; set; }

        public string? AgentAddress { get; set; }

        public AgentModel? Agent { get; set; }

        public string? AvatarAddress { get; set; }

        public AvatarModel? Avatar { get; set; }

        public decimal NcgReward { get; set; }

        public string? RewardData { get; set; }

        public DateOnly Date { get; set; }

        public DateTimeOffset TimeStamp { get; set; }
    }
}
