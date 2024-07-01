namespace NineChronicles.DataProvider.Store.Models.AdventureBoss
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public class AdventureBossSeasonModel
    {
        [Key]
        public int Season { get; set; }

        public long StartBlockIndex { get; set; }

        public long EndBlockIndex { get; set; }

        public long ClaimableBlockIndex { get; set; }

        public long NextSeasonBlockIndex { get; set; }

        public int BossId { get; set; }

        public string? FixedRewardData { get; set; }

        public string? RandomRewardData { get; set; }

        public string? RaffleWinnerAddress { get; set; }

        public AvatarModel? RaffleWinner { get; set; }

        // This is rawValue, so you have to divide by 100 to this reward value
        public decimal RaffleReward { get; set; }

        public DateOnly Date { get; set; }

        public DateTimeOffset TimeStamp { get; set; }
    }
}
