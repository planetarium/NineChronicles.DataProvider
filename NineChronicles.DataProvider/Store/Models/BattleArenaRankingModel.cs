namespace NineChronicles.DataProvider.Store.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public class BattleArenaRankingModel
    {
        public long BlockIndex { get; set; }

        public string? AgentAddress { get; set; }

        [Key]
        public string? AvatarAddress { get; set; }

        public int AvatarLevel { get; set; }

        public int ChampionshipId { get; set; }

        public int Round { get; set; }

        public string? ArenaType { get; set; }

        public int Score { get; set; }

        public int WinCount { get; set; }

        public int MedalCount { get; set; }

        public int LossCount { get; set; }

        public int Ticket { get; set; }

        public int PurchasedTicketCount { get; set; }

        public int TicketResetCount { get; set; }

        public long EntranceFee { get; set; }

        public long TicketPrice { get; set; }

        public long AdditionalTicketPrice { get; set; }

        public int RequiredMedalCount { get; set; }

        public long StartBlockIndex { get; set; }

        public long EndBlockIndex { get; set; }

        public string? Name { get; set; }

        public int? TitleId { get; set; }

        public int? ArmorId { get; set; }

        public int? Cp { get; set; }

        public int Ranking { get; set; }

        public DateTimeOffset TimeStamp { get; set; }
    }
}
