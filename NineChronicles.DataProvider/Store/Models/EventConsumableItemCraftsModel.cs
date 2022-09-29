namespace NineChronicles.DataProvider.Store.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public class EventConsumableItemCraftsModel
    {
        [Key]
        public string? Id { get; set; }

        public string? AgentAddress { get; set; }

        public AgentModel? Agent { get; set; }

        public string? AvatarAddress { get; set; }

        public AvatarModel? Avatar { get; set; }

        public int SlotIndex { get; set; }

        public int EventScheduleId { get; set; }

        public int EventConsumableItemRecipeId { get; set; }

        public int RequiredItem1Id { get; set; }

        public int RequiredItem1Count { get; set; }

        public int RequiredItem2Id { get; set; }

        public int RequiredItem2Count { get; set; }

        public int RequiredItem3Id { get; set; }

        public int RequiredItem3Count { get; set; }

        public int RequiredItem4Id { get; set; }

        public int RequiredItem4Count { get; set; }

        public int RequiredItem5Id { get; set; }

        public int RequiredItem5Count { get; set; }

        public int RequiredItem6Id { get; set; }

        public int RequiredItem6Count { get; set; }

        public long BlockIndex { get; set; }

        public DateTimeOffset Timestamp { get; set; }
    }
}
