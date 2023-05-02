namespace NineChronicles.DataProvider.Store.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public class BattleGrandFinaleModel
    {
        [Key]
        public string? Id { get; set; }

        [Required]
        public long BlockIndex { get; set; }

        [Required]
        public string? AgentAddress { get; set; }

        public AgentModel? Agent { get; set; }

        [Required]
        public string? AvatarAddress { get; set; }

        public AvatarModel? Avatar { get; set; }

        [Required]
        public int AvatarLevel { get; set; }

        [Required]
        public string? EnemyAvatarAddress { get; set; }

        [Required]
        public int GrandFinaleId { get; set; }

        [Required]
        public bool Victory { get; set; }

        [Required]
        public int GrandFinaleScore { get; set; }

        [Required]
        public DateTimeOffset Date { get; set; }

        [Required]
        public DateTimeOffset TimeStamp { get; set; }
    }
}
