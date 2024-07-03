namespace NineChronicles.DataProvider.Store.Models.AdventureBoss
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Numerics;

    public class AdventureBossUnlockFloorModel
    {
        [Key]
        public string? Id { get; set; }

        public long BlockIndex { get; set; }

        public long Season { get; set; }

        public string? AvatarAddress { get; set; }

        public AvatarModel? Avatar { get; set; }

        public int UnlockFloor { get; set; }

        public long UsedGoldenDust { get; set; }

        public decimal UsedNcg { get; set; }

        public long TotalUsedGoldenDust { get; set; }

        public decimal TotalUsedNcg { get; set; }

        public DateOnly Date { get; set; }

        public DateTimeOffset TimeStamp { get; set; }
    }
}
