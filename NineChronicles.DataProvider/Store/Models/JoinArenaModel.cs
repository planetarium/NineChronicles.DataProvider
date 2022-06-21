namespace NineChronicles.DataProvider.Store.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public class JoinArenaModel
    {
        [Key]
        public string? Id { get; set; }

        public long BlockIndex { get; set; }

        public string? AgentAddress { get; set; }

        public AgentModel? Agent { get; set; }

        public string? AvatarAddress { get; set; }

        public AvatarModel? Avatar { get; set; }

        public int AvatarLevel { get; set; }

        public int ArenaRound { get; set; }

        public int ChampionshipId { get; set; }

        public decimal BurntCrystal { get; set; }

        public DateTimeOffset TimeStamp { get; set; }
    }
}
