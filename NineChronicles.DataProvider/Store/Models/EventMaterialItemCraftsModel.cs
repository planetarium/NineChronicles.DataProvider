namespace NineChronicles.DataProvider.Store.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public class EventMaterialItemCraftsModel
    {
        [Key]
        public string? Id { get; set; }

        [Required]
        public string? AgentAddress { get; set; }

        public AgentModel? Agent { get; set; }

        [Required]
        public string? AvatarAddress { get; set; }

        public AvatarModel? Avatar { get; set; }

        [Required]
        public int EventScheduleId { get; set; }

        [Required]
        public int EventMaterialItemRecipeId { get; set; }

        [Required]
        public int Material1Id { get; set; }

        [Required]
        public int Material1Count { get; set; }

        [Required]
        public int Material2Id { get; set; }

        [Required]
        public int Material2Count { get; set; }

        [Required]
        public int Material3Id { get; set; }

        [Required]
        public int Material3Count { get; set; }

        [Required]
        public int Material4Id { get; set; }

        [Required]
        public int Material4Count { get; set; }

        [Required]
        public int Material5Id { get; set; }

        [Required]
        public int Material5Count { get; set; }

        [Required]
        public int Material6Id { get; set; }

        [Required]
        public int Material6Count { get; set; }

        [Required]
        public int Material7Id { get; set; }

        [Required]
        public int Material7Count { get; set; }

        [Required]
        public int Material8Id { get; set; }

        [Required]
        public int Material8Count { get; set; }

        [Required]
        public int Material9Id { get; set; }

        [Required]
        public int Material9Count { get; set; }

        [Required]
        public int Material10Id { get; set; }

        [Required]
        public int Material10Count { get; set; }

        [Required]
        public int Material11Id { get; set; }

        [Required]
        public int Material11Count { get; set; }

        [Required]
        public int Material12Id { get; set; }

        [Required]
        public int Material12Count { get; set; }

        [Required]
        public long BlockIndex { get; set; }

        [Required]
        public DateTimeOffset Date { get; set; }

        [Required]
        public DateTimeOffset Timestamp { get; set; }
    }
}
