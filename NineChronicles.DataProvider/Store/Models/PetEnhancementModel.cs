namespace NineChronicles.DataProvider.Store.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public class PetEnhancementModel
    {
        [Key]
        public string? Id { get; set; }

        public long BlockIndex { get; set; }

        public string? AgentAddress { get; set; }

        public AgentModel? Agent { get; set; }

        public string? AvatarAddress { get; set; }

        public AvatarModel? Avatar { get; set; }

        public int PetId { get; set; }

        public int PreviousPetLevel { get; set; }

        public int TargetLevel { get; set; }

        public int OutputPetLevel { get; set; }

        public int ChangedLevel { get; set; }

        public decimal BurntNCG { get; set; }

        public decimal BurntSoulStone { get; set; }

        public DateOnly Date { get; set; }

        public DateTimeOffset TimeStamp { get; set; }
    }
}
