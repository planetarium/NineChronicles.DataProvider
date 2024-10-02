namespace NineChronicles.DataProvider.Store.Models.Crafting
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public class CustomEquipmentCraftModel
    {
        [Key]
        public string? Id { get; set; }

        // Common
        public long BlockIndex { get; set; }

        public string? AgentAddress { get; set; }

        public AgentModel? Agent { get; set; }

        public string? AvatarAddress { get; set; }

        public AvatarModel? Avatar { get; set; }

        // Input
        public int SlotIndex { get; set; }

        public int RecipeId { get; set; }

        public int Relationship { get; set; }

        public int Scroll { get; set; }

        public int Circle { get; set; }

        public decimal NcgCost { get; set; }

        public string? AdditionalCost { get; set; }

        // Result
        public int EquipmentItemId { get; set; }

        public string? ItemSubType { get; set; }

        public string? ElementalType { get; set; }

        public int IconId { get; set; }

        public long TotalCP { get; set; }

        public int OptionId { get; set; }

        public bool CraftWithRandom { get; set; }

        public bool HasRandomOnlyIcon { get; set; }

        // Time
        public DateOnly Date { get; set; }

        public DateTimeOffset TimeStamp { get; set; }
    }
}
