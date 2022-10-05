namespace NineChronicles.DataProvider.Store.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public class EventDungeonBattleModel
    {
        [Key]
        public string? Id { get; set; }

        public string? AgentAddress { get; set; }

        public AgentModel? Agent { get; set; }

        public string? AvatarAddress { get; set; }

        public AvatarModel? Avatar { get; set; }

        public int EventDungeonId { get; set; }

        public int EventScheduleId { get; set; }

        public int EventDungeonStageId { get; set; }

        public int RemainingTickets { get; set; }

        public decimal BurntNCG { get; set; }

        public bool Cleared { get; set; }

        public int FoodsCount { get; set; }

        public int CostumesCount { get; set; }

        public int EquipmentsCount { get; set; }

        public int RewardItem1Id { get; set; }

        public int RewardItem1Count { get; set; }

        public int RewardItem2Id { get; set; }

        public int RewardItem2Count { get; set; }

        public int RewardItem3Id { get; set; }

        public int RewardItem3Count { get; set; }

        public int RewardItem4Id { get; set; }

        public int RewardItem4Count { get; set; }

        public int RewardItem5Id { get; set; }

        public int RewardItem5Count { get; set; }

        public int RewardItem6Id { get; set; }

        public int RewardItem6Count { get; set; }

        public int RewardItem7Id { get; set; }

        public int RewardItem7Count { get; set; }

        public int RewardItem8Id { get; set; }

        public int RewardItem8Count { get; set; }

        public int RewardItem9Id { get; set; }

        public int RewardItem9Count { get; set; }

        public int RewardItem10Id { get; set; }

        public int RewardItem10Count { get; set; }

        public long BlockIndex { get; set; }

        public DateTimeOffset Timestamp { get; set; }
    }
}
