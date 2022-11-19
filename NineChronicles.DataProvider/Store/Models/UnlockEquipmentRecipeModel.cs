namespace NineChronicles.DataProvider.Store.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public class UnlockEquipmentRecipeModel
    {
        [Key]
        public string? Id { get; set; }

        public long BlockIndex { get; set; }

        public string? AgentAddress { get; set; }

        public AgentModel? Agent { get; set; }

        public string? AvatarAddress { get; set; }

        public AvatarModel? Avatar { get; set; }

        public int UnlockEquipmentRecipeId { get; set; }

        public decimal BurntCrystal { get; set; }

        public DateTimeOffset TimeStamp { get; set; }
    }
}
