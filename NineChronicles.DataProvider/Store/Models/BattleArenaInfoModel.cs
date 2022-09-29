namespace NineChronicles.DataProvider.Store.Models
{
    public class BattleArenaInfoModel
    {
        public int ChampionshipId { get; set; }

        public int Round { get; set; }

        public string? ArenaType { get; set; }

        public long StartBlockIndex { get; set; }

        public long EndBlockIndex { get; set; }

        public int RequiredMedalCount { get; set; }

        public long EntranceFee { get; set; }

        public long TicketPrice { get; set; }

        public long AdditionalTicketPrice { get; set; }

        public long QueryBlockIndex { get; set; }

        public long StoreTipBlockIndex { get; set; }
    }
}
