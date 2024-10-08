namespace NineChronicles.DataProvider.Store.Models.Crafting
{
    using System.ComponentModel.DataAnnotations;

    public class CustomEquipmentCraftCountModel
    {
        [Key]
        public int IconId { get; set; }

        public string? ItemSubType { get; set; }

        public long Count { get; set; }
    }
}
