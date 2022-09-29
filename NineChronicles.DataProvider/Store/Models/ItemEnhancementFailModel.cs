namespace NineChronicles.DataProvider.Store.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public class ItemEnhancementFailModel
    {
        [Key]
        public string? Id { get; set; }

        public long BlockIndex { get; set; }

        public string? AgentAddress { get; set; }

        public AgentModel? Agent { get; set; }

        public string? AvatarAddress { get; set; }

        public AvatarModel? Avatar { get; set; }

        public string? EquipmentItemId { get; set; }

        public string? MaterialItemId { get; set; }

        public int EquipmentLevel { get; set; }

        public decimal GainedCrystal { get; set; }

        public decimal BurntNCG { get; set; }

        public DateTimeOffset TimeStamp { get; set; }
    }
}
