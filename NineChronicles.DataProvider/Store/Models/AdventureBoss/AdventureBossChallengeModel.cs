namespace NineChronicles.DataProvider.Store.Models.AdventureBoss
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public class AdventureBossChallengeModel
    {
        [Key]
        public string? Id { get; set; }

        public long BlockIndex { get; set; }

        public long Season { get; set; }

        public string? AgentAddress { get; set; }

        public AgentModel? Agent { get; set; }

        public string? AvatarAddress { get; set; }

        public AvatarModel? Avatar { get; set; }

        public int StartFloor { get; set; }

        public int EndFloor { get; set; }

        public int UsedApPotion { get; set; }

        public int Point { get; set; }

        public long TotalPoint { get; set; }

        public DateOnly Date { get; set; }

        public DateTimeOffset TimeStamp { get; set; }
    }
}
