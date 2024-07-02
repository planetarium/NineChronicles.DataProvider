namespace NineChronicles.DataProvider.Store.Models.AdventureBoss
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public class AdventureBossWantedModel
    {
        [Key]
        public string? Id { get; set; }

        public long BlockIndex { get; set; }

        public int Season { get; set; }

        public string? AvatarAddress { get; set; }

        public AvatarModel? Avatar { get; set; }

        public decimal Bounty { get; set; }

        public int Count { get; set; }

        public decimal TotalBounty { get; set; }

        public DateOnly Date { get; set; }

        public DateTimeOffset TimeStamp { get; set; }
    }
}
