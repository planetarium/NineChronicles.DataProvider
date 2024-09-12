namespace NineChronicles.DataProvider.Store.Models.Crafting
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public class CustomEquipmentCraftModel
    {
        [Key]
        public string? Id { get; set; }

        public long BlockIndex { get; set; }

        public string? AvatarAddress { get; set; }

        public AvatarModel? Avatar { get; set; }

        public int EquipmentItemId { get; set; }

        public int RecipeId { get; set; }

        public int SlotIndex { get; set; }

        public string? ItemSubType { get; set; }

        public int IconId { get; set; }

        public string? ElementalType { get; set; }

        public int DrawingAmount { get; set; }

        public int DrawingToolAmount { get; set; }

        public decimal NcgCost { get; set; }

        public string? AdditionalCost { get; set; }

        public DateOnly Date { get; set; }

        public DateTimeOffset TimeStamp { get; set; }
    }
}
