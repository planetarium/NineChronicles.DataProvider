namespace NineChronicles.DataProvider.Store.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public class BattleArenaModel
    {
        [Key]
        public string? Id { get; set; }

        public long BlockIndex { get; set; }

        public string? AgentAddress { get; set; }

        public AgentModel? Agent { get; set; }

        public string? AvatarAddress { get; set; }

        public AvatarModel? Avatar { get; set; }

        public int AvatarLevel { get; set; }

        public string? EnemyAvatarAddress { get; set; }

        public int ChampionshipId { get; set; }

        public int Round { get; set; }

        public int TicketCount { get; set; }

        public decimal BurntNCG { get; set; }

        public bool Victory { get; set; }

        public int MedalCount { get; set; }

        public DateTimeOffset TimeStamp { get; set; }
    }
}
