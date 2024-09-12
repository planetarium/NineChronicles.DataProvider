namespace NineChronicles.DataProvider.Store.Models.Crafting
{
    public class UnlockCombinationSlotModel : BaseModel, IAvatar
    {
        public string? AvatarAddress { get; set; }

        public AvatarModel? Avatar { get; set; }

        public int SlotIndex { get; set; }

        public decimal NcgCost { get; set; }

        public decimal CrystalCost { get; set; }

        public string? MaterialCosts { get; set; }
    }
}
