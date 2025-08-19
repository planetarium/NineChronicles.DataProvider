namespace NineChronicles.DataProvider.Store.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public class EquipmentModel
    {
        [Key]
        public string? ItemId { get; set; }

        public string? AgentAddress { get; set; }

        public string? AvatarAddress { get; set; }

        public int EquipmentId { get; set; }

        public long Cp { get; set; }

        public int Level { get; set; }

        public string? ItemSubType { get; set; }

        public DateTimeOffset TimeStamp { get; set; }
    }
}
